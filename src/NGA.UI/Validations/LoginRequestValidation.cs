using FluentValidation;
using NGA.UI.Model.Request;

namespace NGA.UI.Validations
{
    public class LoginRequestValidation : AbstractValidator<LoginRequest>
    {
        public LoginRequestValidation()
        {
            RuleFor(x => x.Phone)
                .NotEmpty()
                .MaximumLength(50);
            RuleFor(x => x.Code)
               .NotEmpty()
               .MaximumLength(10);
        }
    }
}
