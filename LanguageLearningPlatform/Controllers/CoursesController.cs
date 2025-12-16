using Microsoft.AspNetCore.Mvc;

namespace LanguageLearningPlatform.Web.Controllers
{
    public class CourseController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
