using Microsoft.AspNetCore.Mvc;

namespace LanguageLearningPlatform.Web.Controllers
{
    public class LessonsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
