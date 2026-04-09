using FluentValidation;

namespace ASB.POC.Application.Messages.Commands.SendMessage;

public sealed class SendMessageCommandValidator : AbstractValidator<SendMessageCommand>
{
    public SendMessageCommandValidator()
    {
        RuleFor(x => x.Subject)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(x => x.Body)
            .NotEmpty()
            .MaximumLength(256_000);
    }
}
