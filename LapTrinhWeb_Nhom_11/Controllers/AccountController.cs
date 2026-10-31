using LapTrinhWeb_Nhom_11.DTO;
using LapTrinhWeb_Nhom_11.Helpers;
using LapTrinhWeb_Nhom_11.Models;
using Microsoft.AspNetCore.Mvc;

namespace LapTrinhWeb_Nhom_11.Controllers
{
    public class AccountController : Controller
    {
        // Trang đăng nhập
        [HttpGet("/login")]
        public IActionResult Login()
        {
            return View();
        }

        // Đăng nhập: admin thì chuyển sang trang quản trị, khách hàng thì về trang bán hàng
        [HttpPost("/login")]
        public IActionResult DoLogin([FromBody] LoginDTO dto)
        {
            AppDbContext appDbContext = new AppDbContext();
            string passwordHash = PasswordHelper.Hash(dto.Password);
            var account = appDbContext.Accounts
                .Where(c => c.Username == dto.Username && c.PasswordHash == passwordHash)
                .FirstOrDefault();
            if (account == null)
            {
                return BadRequest("Sai tên đăng nhập hoặc mật khẩu");
            }
            if (account.IsActive == false)
            {
                return BadRequest("Tài khoản đã bị khóa");
            }
            HttpContext.Session.SetInt32("AccountId", account.Id);
            HttpContext.Session.SetString("Role", account.Role);
            HttpContext.Session.SetString("FullName", account.FullName ?? account.Username);

            string redirect = "/";
            if (account.Role == "ADMIN")
            {
                redirect = "/admin";
            }
            return Ok(new { redirect = redirect });
        }

        // Đăng xuất
        [HttpGet("/logout")]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return Redirect("/login");
        }
    }
}