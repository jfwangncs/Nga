using NGA.UI.Extensions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NGA.UI.Model
{
    public class BaseResponse<T>
    {
        public ResponseCode Code { get; set; } = ResponseCode.Success;
        public string Message { get; set; } = NGA.UI.Extensions.ErrorCode.OutOfSotck.GetDescription();
        public ErrorCode? ErrorCode { get; set; }
        public T? Data { get; set; }
    }
}
