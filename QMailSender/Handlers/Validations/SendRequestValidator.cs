using FluentValidation;
using QMailSender.Models;

namespace QMailSender.Handlers.Validations;

public class SendRequestValidator : AbstractValidator<SendRequest>
{
    public SendRequestValidator()
    {
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(200).MinimumLength(10);
        RuleFor(x => x.FromAddress).NotEmpty().EmailAddress();
        RuleFor(x => x.FromName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.SendDelay).GreaterThan(500);
        RuleFor(x => x.TargetAddresses).NotEmpty();
        RuleFor(x => x.SmtpSettings).SetValidator(new SmtpSettingsValidator());
        RuleForEach(x => x.TargetAddresses).NotEmpty().EmailAddress();
    }
}