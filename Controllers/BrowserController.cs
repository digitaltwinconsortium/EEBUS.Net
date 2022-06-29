
namespace EEBUS.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using System;

    public class BrowserController : Controller
    {
        private readonly MDNSClient _mDNSClient;

        public BrowserController(MDNSClient mDNSClient)
        {
            _mDNSClient = mDNSClient;
        }

        public IActionResult Index()
        {
            return View("Index", _mDNSClient.getEEBUSNodes());
        }

        [HttpPost]
        public IActionResult Connect()
        {
            try
            {
                foreach (string key in Request.Form.Keys)
                {
                    if (key.Contains("Endpoint:"))
                    {
                        string[] parts = key.Split(' ');
                        break;
                    }
                }

                return View("Index", _mDNSClient.getEEBUSNodes());
            }
            catch (Exception ex)
            {
                return View("Index", new string[] { "Error: " + ex.Message });
            }
        }
    }
}
