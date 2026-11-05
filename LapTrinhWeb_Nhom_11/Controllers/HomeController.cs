using Microsoft.AspNetCore.Mvc;

namespace LapTrinhWeb_Nhom_11.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
