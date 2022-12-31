using FluentValidation;
using QMailSender.Handlers.Commands;

namespace QMailSender.Handlers.Validations;

public class SendCommandValidator : AbstractValidator<SendCommand>
{
    public SendCommandValidator()
    {
        RuleFor(x => x.SendRequest).SetValidator(new SendRequestValidator());
    }
}