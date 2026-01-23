using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace NGA.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    public class HealthController(ILogger<HealthController> logger) : ControllerBase
    {
        private readonly ILogger<HealthController> _logger = logger;

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Get()
        {
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var result = "V1:Ok";

            _logger.LogInformation("Date:{Date},Env:{Env}", DateTime.Now, env);

            return Ok(result);
        }
    }
}
