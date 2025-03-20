using Microsoft.AspNetCore.Mvc;

namespace AgroMarket.Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok("Test endpoint is working!");
        }
    }
}