using LapTrinhWeb_Nhom_11.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace LapTrinhWeb_Nhom_11.Controllers
{
    // Trang chủ quản trị, chỉ admin mới vào được
    [AdminOnly]
    [Route("/admin")]
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}