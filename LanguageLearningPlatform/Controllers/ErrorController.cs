using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace LanguageLearningPlatform.Web.Controllers
{
    public class ErrorController : Controller
    {
        [Route("Error/{statusCode}")]
        public IActionResult HttpStatusCodeHandler(int statusCode)
        {
            var statusCodeResult = HttpContext.Features.Get<IStatusCodeReExecuteFeature>();

            ViewBag.OriginalPath = statusCodeResult?.OriginalPath ?? "";

            return statusCode switch
            {
                400 => View("Error400"),
                401 => View("Error401"),
                403 => View("Error403"),
                404 => View("Error404"),
                503 => View("Error503"),
                _ => View("Error500")
            };
        }

        [Route("Error")]
        public IActionResult Index()
        {
            var exceptionDetails = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            ViewBag.ExceptionPath = exceptionDetails?.Path ?? "";
            return View("Error500");
        }
    }
}   