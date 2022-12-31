using FluentValidation;
using QMailSender.Models;

namespace QMailSender.Handlers.Validations;

public class SmtpSettingsValidator : AbstractValidator<SmtpSettings> 
{
    public SmtpSettingsValidator()
    {
        RuleFor(x => x.SmtpServer).NotEmpty();
        RuleFor(x => x.Username).NotEmpty();
        RuleFor(x => x.Password).NotEmpty();
        RuleFor(x => x.SmtpPort).GreaterThan(0);
    }
}