using NGA.Api.Extensions;

namespace NGA.Api.Model
{
    public class BaseResponse<T>
    {
        public ResponseCode Code { get; set; } = ResponseCode.Success;
        public string Message { get; set; } = ResponseCode.Success.GetDescription();
        public ErrorCode? ErrorCode { get; set; }
        public T? Data { get; set; }
    }
}
