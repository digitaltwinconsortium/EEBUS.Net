
namespace EEBUS.Controllers
{
    using Microsoft.AspNetCore.Mvc;

    public class BrowserController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
