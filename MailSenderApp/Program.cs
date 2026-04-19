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
    Attachments = []
    // 例:
    // AttachmentPaths = ["files/report.pdf", "files/image.png"]
};

await mailService.SendAsync(request);


var pdfBytes = await File.ReadAllBytesAsync("docs/report.pdf");

var request2 = new MailRequest
{
    To = ["to@example.com"],
    Subject = "byte[] 添付テスト",
    TextBody = "PDFを添付しています。",
    HtmlBody = "<p>PDFを添付しています。</p>",
    Priority = MailPriorityLevel.Normal,
    Attachments =
    [
        MailAttachment.FromBytes(
            fileName: "report.pdf",
            data: pdfBytes,
            contentType: "application/pdf")
    ]
};

await mailService.SendAsync(request2);


await using var stream = File.OpenRead("images/sample.png");

var request3 = new MailRequest
{
    To = ["to@example.com"],
    Subject = "Stream 添付テスト",
    TextBody = "画像を添付しています。",
    HtmlBody = "<p>画像を添付しています。</p>",
    Priority = MailPriorityLevel.Normal,
    Attachments =
    [
        MailAttachment.FromStream(
            fileName: "sample.png",
            contentStream: stream,
            contentType: "image/png")
    ]
};

await mailService.SendAsync(request3);

Console.WriteLine("送信完了");