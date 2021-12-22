using ExternalLogic;
using ExternalLogic.Models;
using Microsoft.AspNetCore.Mvc;

namespace RabbitMQDemo.Controllers
{
    public class HomeController : Controller
    {
        private readonly IEmailingService _emailingService;
        private readonly ISupportService _supportService;

        public HomeController(IEmailingService emailingService, ISupportService supportService)
        {
            _emailingService = emailingService;
            _supportService = supportService;
        }

        public JsonResult GetMessagingProgress(string id)
        {
            ProgressModel? res = _emailingService.GetProgress(id);

            return new JsonResult(new
            {
                total = res.Total,
                processed = res.Processed + res.Failed,
                errors = res.Failed
            });
        }

        public JsonResult IsConnectionAlive()
        {
            bool res = false;

            string? key = Request.Cookies["support-session"];
            if (!string.IsNullOrEmpty(key))
            {
                res = _supportService.IsConnectionAlive(key);
            }

            return new JsonResult(new
            {
                result = res
            });
        }

        public ActionResult SendNack(ulong id)
        {
            string? key = Request.Cookies["support-session"];
            if (string.IsNullOrEmpty(key)) return BadRequest();

            _supportService.DiscardMessage(key, id);

            return Ok();
        }
    }
}
