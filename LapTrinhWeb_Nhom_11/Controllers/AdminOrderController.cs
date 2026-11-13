using LapTrinhWeb_Nhom_11.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace LapTrinhWeb_Nhom_11.Controllers
{
    [AdminOnly]
    [Route("/admin/orders")]
    public class AdminOrderController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
