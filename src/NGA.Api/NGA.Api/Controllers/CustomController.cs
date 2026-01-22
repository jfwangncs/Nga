using Microsoft.AspNetCore.Mvc;
using NGA.Api.Extensions;
using NGA.Api.Model;
using System.Net;

namespace NGA.Api.Controllers
{
    public class CustomController : ControllerBase
    {
        protected new OkObjectResult Ok()
        {
            var resp = new BaseResponse<string>()
            {
                Code = ResponseCode.Success,
                Message = ResponseCode.Success.GetDescription(),
            };
            return base.Ok(resp);
        }

        protected OkObjectResult Ok<T>(T data)
        {
            var resp = new BaseResponse<object>()
            {
                Code = ResponseCode.Success,
                Message = ResponseCode.Success.GetDescription(),
                Data = data
            };
            return base.Ok(resp);
        }
        protected OkObjectResult Ok<T>(string message, T data)
        {
            var resp = new BaseResponse<object>()
            {
                Code = ResponseCode.Success,
                Message = message,
                Data = data
            };
            return base.Ok(resp);
        }
        protected BadRequestObjectResult BadRequest(ResponseCode responseCode, ErrorCode? errorCode, string? message = null)
        {
            var resp = new BaseResponse<object>()
            {
                Code = responseCode,
                ErrorCode = errorCode,
                Message = message ?? errorCode?.GetDescription() ?? responseCode.GetDescription(),
            };
            return base.BadRequest(resp);
        }

        protected BadRequestObjectResult BadRequest(ErrorCode errorCode, string? message = null)
        {
            var resp = new BaseResponse<object>()
            {
                Code = ResponseCode.Failed,
                ErrorCode = errorCode,
                Message = message ?? errorCode.GetDescription(),
            };
            return base.BadRequest(resp);
        }

        protected BadRequestObjectResult BadRequest<T>(ResponseCode responseCode, ErrorCode? errorCode, T data, string? message = null)
        {
            var resp = new BaseResponse<object>()
            {
                Code = responseCode,
                ErrorCode = errorCode,
                Message = message ?? errorCode?.GetDescription() ?? responseCode.GetDescription(),
                Data = data
            };
            return base.BadRequest(resp);
        }

        protected BadRequestObjectResult BadRequest<T>(ErrorCode errorCode, T data, string? message = null)
        {
            var resp = new BaseResponse<object>()
            {
                Code = ResponseCode.Failed,
                ErrorCode = errorCode,
                Message = message ?? errorCode.GetDescription() ?? ResponseCode.Failed.GetDescription(),
                Data = data
            };
            return base.BadRequest(resp);
        }
        protected ObjectResult Result<T>(ResponseCode code, ErrorCode errorCode, T data, HttpStatusCode httpStatus, string? message = null)
        {
            var resp = new BaseResponse<object>()
            {
                Code = ResponseCode.Failed,
                ErrorCode = errorCode,
                Message = message ?? errorCode.GetDescription() ?? ResponseCode.Failed.GetDescription(),
                Data = data
            };
            return base.StatusCode((int)httpStatus, resp);
        }

    }
}
