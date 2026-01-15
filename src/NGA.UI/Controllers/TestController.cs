using JfYu.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace NGA.UI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    { 

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Get()
        { 
            return Ok("Test");
        }
    }
}
