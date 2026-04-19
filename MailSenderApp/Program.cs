using MailSenderApp.Models;
using MailSenderApp.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<MailSettings>(
    builder.Configuration.GetSection(MailSettings.SectionName));

builder.Services.AddLogging();
builder.Services.AddSingleton<IMailService, MailService>();

using var host = builder.Build();

var mailService = host.Services.GetRequiredService<IMailService>();

var request = new MailRequest
{
    To = ["to@example.com"],
    Cc = ["cc@example.com"],
    Bcc = [],
    Subject = "業務向け MailService テンプレート",
    TextBody = """
               これはテキスト本文です。
               HTMLが表示できない環境ではこちらが使われます。
               """,
    HtmlBody = """
               <h1>これはHTML本文です</h1>
               <p><strong>MailService</strong> から送信しています。</p>
               <p>text/plain と text/html の両方を持っています。</p>
               """,
    Priority = MailPriorityLevel.High,
    AttachmentPaths = []
    // 例:
    // AttachmentPaths = ["files/report.pdf", "files/image.png"]
};

await mailService.SendAsync(request);

Console.WriteLine("送信完了");