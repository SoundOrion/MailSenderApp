using System.Threading.Channels;
using MyMailApi.Application.Abstractions;
using MyMailApi.Domain;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MyMailApi.Infrastructure.Queue;

public sealed class QueuedMailWorker : BackgroundService
{
    private readonly ChannelReader<MailMessageData> _reader;
    private readonly IMailSender _mailSender;
    private readonly MailQueueOptions _options;
    private readonly ILogger<QueuedMailWorker> _logger;

    public QueuedMailWorker(
        Channel<MailMessageData> channel,
        IMailSender mailSender,
        IOptions<MailQueueOptions> options,
        ILogger<QueuedMailWorker> logger)
    {
        _reader = channel.Reader;
        _mailSender = mailSender;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("メールキューワーカー開始");

        await foreach (var message in _reader.ReadAllAsync(stoppingToken))
        {
            await ProcessMessageAsync(message, stoppingToken);
        }

        _logger.LogInformation("メールキューワーカー終了");
    }

    private async Task ProcessMessageAsync(
        MailMessageData message,
        CancellationToken cancellationToken)
    {
        Exception? lastException = null;

        for (var attempt = 1; attempt <= _options.MaxRetryCount; attempt++)
        {
            try
            {
                _logger.LogInformation(
                    "キュー送信開始: Subject={Subject}, Attempt={Attempt}/{Max}",
                    message.Subject,
                    attempt,
                    _options.MaxRetryCount);

                await _mailSender.SendAsync(message, cancellationToken);

                _logger.LogInformation(
                    "キュー送信成功: Subject={Subject}",
                    message.Subject);

                return;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(
                    "停止要求によりキュー送信中断: Subject={Subject}",
                    message.Subject);
                throw;
            }
            catch (Exception ex)
            {
                lastException = ex;

                _logger.LogWarning(
                    ex,
                    "キュー送信失敗: Subject={Subject}, Attempt={Attempt}/{Max}",
                    message.Subject,
                    attempt,
                    _options.MaxRetryCount);

                if (attempt < _options.MaxRetryCount)
                {
                    await Task.Delay(
                        TimeSpan.FromSeconds(_options.RetryDelaySeconds),
                        cancellationToken);
                }
            }
        }

        _logger.LogError(
            lastException,
            "キュー送信最終失敗: Subject={Subject}",
            message.Subject);
    }
}