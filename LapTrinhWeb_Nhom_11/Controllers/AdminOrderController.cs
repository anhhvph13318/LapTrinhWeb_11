using LapTrinhWeb_Nhom_11.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace LapTrinhWeb_Nhom_11.Controllers
{
    // Trang quản lý hóa đơn cho admin (dữ liệu lấy từ OrderController)
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
