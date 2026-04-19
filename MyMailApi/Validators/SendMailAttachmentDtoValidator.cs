using FluentValidation;
using MyMailApi.Contracts;

namespace MyMailApi.Validators;

public sealed class SendMailAttachmentDtoValidator : AbstractValidator<SendMailAttachmentDto>
{
    public SendMailAttachmentDtoValidator()
    {
        RuleFor(x => x.FileName)
            .NotEmpty()
            .WithMessage("添付ファイル名は必須です。");

        RuleFor(x => x.Base64Data)
            .NotEmpty()
            .WithMessage("添付ファイルの Base64Data は必須です。");
    }
}