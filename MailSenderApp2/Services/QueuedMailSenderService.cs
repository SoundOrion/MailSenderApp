using System.Threading.Channels;
using MailSenderApp.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MailSenderApp.Services;

public sealed class QueuedMailSenderService : BackgroundService
{
    private readonly ChannelReader<MailRequest> _reader;
    private readonly IMailService _mailService;
    private readonly MailQueueOptions _options;
    private readonly ILogger<QueuedMailSenderService> _logger;

    public QueuedMailSenderService(
        Channel<MailRequest> channel,
        IMailService mailService,
        IOptions<MailQueueOptions> options,
        ILogger<QueuedMailSenderService> logger)
    {
        _reader = channel.Reader;
        _mailService = mailService;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("メールキュー処理サービス開始");

        await foreach (var request in _reader.ReadAllAsync(stoppingToken))
        {
            await ProcessAsync(request, stoppingToken);
        }

        _logger.LogInformation("メールキュー処理サービス終了");
    }

    private async Task ProcessAsync(MailRequest request, CancellationToken cancellationToken)
    {
        Exception? lastException = null;

        for (var attempt = 1; attempt <= _options.MaxRetryCount; attempt++)
        {
            try
            {
                _logger.LogInformation(
                    "メール送信処理開始: Subject={Subject}, Attempt={Attempt}/{Max}",
                    request.Subject,
                    attempt,
                    _options.MaxRetryCount);

                await _mailService.SendAsync(request, cancellationToken);

                _logger.LogInformation(
                    "メール送信処理成功: Subject={Subject}",
                    request.Subject);

                return;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(
                    "停止要求によりメール送信を中断: Subject={Subject}",
                    request.Subject);
                throw;
            }
            catch (Exception ex)
            {
                lastException = ex;

                _logger.LogWarning(
                    ex,
                    "メール送信失敗: Subject={Subject}, Attempt={Attempt}/{Max}",
                    request.Subject,
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
            "メール送信最終失敗: Subject={Subject}",
            request.Subject);
    }
}