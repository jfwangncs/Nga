using System.ComponentModel;

namespace NGA.UI.Extensions
{
    public enum ErrorCode
    {
        //unexpected  error 
        [Description("System error.")]
        SystemError = 1000,

        //client error
        [Description("Validation error.")]
        ValidationError = 4000,
        [Description("Token is Unauthorized.")]
        UnauthorizedError,
        [Description("Resources is forbidden.")]
        ForbiddenError,
        [Description("Invalid Credentials.")]
        InvalidCredentials,

        //internal service error
        [Description("The product is out of stock")]
        OutOfSotck = 5000
    }
}
