using LapTrinhWeb_Nhom_11.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace LapTrinhWeb_Nhom_11.Controllers
{
    // Trang quản lý tài khoản cho admin (dữ liệu lấy từ AccountController)
    [AdminOnly]
    [Route("/admin/accounts")]
    public class AdminAccountController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}