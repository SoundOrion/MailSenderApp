using System.Threading.Channels;
using MyMailApi.Application.Abstractions;
using MyMailApi.Application.Services;
using MyMailApi.Domain;
using MyMailApi.Endpoints;
using MyMailApi.Infrastructure.Mail;
using MyMailApi.Infrastructure.Queue;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<MailSettings>(
    builder.Configuration.GetSection(MailSettings.SectionName));

builder.Services.Configure<MailQueueOptions>(
    builder.Configuration.GetSection(MailQueueOptions.SectionName));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IMailSender, MailKitMailSender>();

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

    return Channel.CreateBounded<MailMessageData>(channelOptions);
});

builder.Services.AddSingleton<IMailQueue, MailQueue>();
builder.Services.AddHostedService<QueuedMailWorker>();

// ’¼ÚƒAƒvƒŠ‘w‚©‚ç IMailDispatcher ‚ğg‚¢‚½‚¢ê‡‚ÌŠù’è“o˜^
var defaultMode = builder.Configuration["MailDispatch:DefaultMode"] ?? "Direct";
if (string.Equals(defaultMode, "Queued", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddSingleton<IMailDispatcher, QueuedMailDispatcher>();
}
else
{
    builder.Services.AddSingleton<IMailDispatcher, DirectMailDispatcher>();
}

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapMailEndpoints();

app.Run();