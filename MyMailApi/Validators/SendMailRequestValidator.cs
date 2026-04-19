using FluentValidation;
using MyMailApi.Contracts;

namespace MyMailApi.Validators;

public sealed class SendMailRequestValidator : AbstractValidator<SendMailRequest>
{
    public SendMailRequestValidator()
    {
        RuleFor(x => x.Subject)
            .NotEmpty()
            .WithMessage("件名は必須です。");

        RuleFor(x => x)
            .Must(x =>
                (x.To?.Count ?? 0) > 0 ||
                (x.Cc?.Count ?? 0) > 0 ||
                (x.Bcc?.Count ?? 0) > 0)
            .WithMessage("To, Cc, Bcc のいずれかに最低1件の宛先が必要です。");

        RuleFor(x => x)
            .Must(x =>
                !string.IsNullOrWhiteSpace(x.TextBody) ||
                !string.IsNullOrWhiteSpace(x.HtmlBody))
            .WithMessage("TextBody または HtmlBody のどちらかは必須です。");

        RuleForEach(x => x.To)
            .EmailAddress()
            .WithMessage("To に不正なメールアドレスがあります。");

        RuleForEach(x => x.Cc)
            .EmailAddress()
            .WithMessage("Cc に不正なメールアドレスがあります。");

        RuleForEach(x => x.Bcc)
            .EmailAddress()
            .WithMessage("Bcc に不正なメールアドレスがあります。");

        RuleForEach(x => x.Attachments)
            .SetValidator(new SendMailAttachmentDtoValidator());
    }
}