using FluentValidation; 
using NGA.Api.Model.Request;

namespace NGA.Api.Validations
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
