using System.Threading.Channels;
using MailSenderApp.Models;
using MailSenderApp.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<MailSettings>(
    builder.Configuration.GetSection(MailSettings.SectionName));

builder.Services.Configure<MailQueueOptions>(
    builder.Configuration.GetSection(MailQueueOptions.SectionName));

builder.Services.AddSingleton(sp =>
{
    var options = sp.GetRequiredService<
        Microsoft.Extensions.Options.IOptions<MailQueueOptions>>().Value;

    var channelOptions = new BoundedChannelOptions(options.Capacity)
    {
        FullMode = BoundedChannelFullMode.Wait,
        SingleReader = true,
        SingleWriter = false
    };

    return Channel.CreateBounded<MailRequest>(channelOptions);
});

builder.Services.AddSingleton<IMailQueue, MailQueue>();
builder.Services.AddSingleton<IMailService, MailService>();
builder.Services.AddHostedService<QueuedMailSenderService>();

using var host = builder.Build();

await host.StartAsync();

var queue = host.Services.GetRequiredService<IMailQueue>();

var pdfBytes = await File.ReadAllBytesAsync("docs/report.pdf");

await queue.QueueAsync(new MailRequest
{
    To = ["to1@example.com"],
    Subject = "キュー送信 1",
    TextBody = "これはキュー経由のメールです。",
    HtmlBody = "<p>これはキュー経由のメールです。</p>",
    Priority = MailPriorityLevel.Normal
});

await queue.QueueAsync(new MailRequest
{
    To = ["to2@example.com"],
    Subject = "キュー送信 2",
    TextBody = "PDF添付ありです。",
    HtmlBody = "<p>PDF添付ありです。</p>",
    Priority = MailPriorityLevel.High,
    Attachments =
    [
        MailAttachment.FromBytes("report.pdf", pdfBytes, "application/pdf")
    ]
});

Console.WriteLine("メールをキューに投入しました。Enterで終了します。");
Console.ReadLine();

await host.StopAsync();