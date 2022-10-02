using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ProtectedWeb.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HomeController : ControllerBase
    {

        public HomeController()
        {
        }

        [HttpGet("[action]")]
        public IActionResult Index()
        {
            return Ok("Index");
        }

        [HttpGet("[action]")]
        public IActionResult Privacy()
        {
            return Ok("Privacy");
        }
    }
}
