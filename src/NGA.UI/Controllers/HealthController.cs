using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace NGA.UI.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    public class HealthController : ControllerBase
    {
        private readonly ILogger<HealthController> _logger;

        public HealthController(ILogger<HealthController> logger) => _logger = logger;

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Get()
        {
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var version = System.IO.File.ReadAllText($"{Environment.CurrentDirectory}/version.txt");
            var result = "V1\r\n";

            result += $"\r\nDatetime:{DateTime.Now}";

            result += $"\r\nEnv:{env} ";

            result += $"\r\nVersion:{version}";

            _logger.LogInformation("Version:{Version},Date:{Date},Env:{Env}", version, DateTime.Now, env);

            return Ok(result);
        }
    }
}
