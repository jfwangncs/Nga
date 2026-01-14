using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using static System.Runtime.InteropServices.JavaScript.JSType;

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

        //internal service error
        [Description("The product is out of stock")] 
        OutOfSotck = 5000
    }
}
