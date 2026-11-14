using LapTrinhWeb_Nhom_11.DTO;
using LapTrinhWeb_Nhom_11.Helpers;
using LapTrinhWeb_Nhom_11.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

namespace LapTrinhWeb_Nhom_11.Controllers
{
    [LoginRequired]
    public class ProfileController : Controller
    {
        [HttpGet("/profile")]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("/order-history")]
        public IActionResult OrderHistory()
        {
            return View();
        }

        [HttpGet("/profile/data")]
        public IActionResult GetData()
        {
            int accountId = HttpContext.Session.GetInt32("AccountId") ?? 0;
            AppDbContext appDbContext = new AppDbContext();
            var account = appDbContext.Accounts.Where(c => c.Id == accountId).FirstOrDefault();
            if (account == null)
            {
                return NotFound("Không tìm thấy tài khoản");
            }
            ProfileDTO profile = new ProfileDTO();
            profile.Username = account.Username;
            profile.FullName = account.FullName;
            profile.Email = account.Email;
            profile.Phone = account.Phone;
            return Ok(profile);
        }

        [HttpPut("/profile/update")]
        public IActionResult Update([FromBody] ProfileDTO dto)
        {
            int accountId = HttpContext.Session.GetInt32("AccountId") ?? 0;
            AppDbContext appDbContext = new AppDbContext();
            var account = appDbContext.Accounts.Where(c => c.Id == accountId).FirstOrDefault();
            if (account == null)
            {
                return NotFound("Không tìm thấy tài khoản");
            }
            string? fullName = Clean(dto.FullName);
            if (fullName == null)
            {
                return BadRequest("Họ tên không được để trống");
            }
            if (fullName.Length > 100)
            {
                return BadRequest("Họ tên tối đa 100 ký tự");
            }
            string? email = Clean(dto.Email);
            if (email != null)
            {
                if (email.Length > 100 || Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$") == false)
                {
                    return BadRequest("Email không hợp lệ");
                }
                var sameEmail = appDbContext.Accounts.Where(c => c.Email == email && c.Id != accountId).FirstOrDefault();
                if (sameEmail != null)
                {
                    return BadRequest("Email đã được dùng cho tài khoản khác");
                }
            }
            string? phone = Clean(dto.Phone);
            if (phone != null && Regex.IsMatch(phone, @"^\+?[0-9]{9,15}$") == false)
            {
                return BadRequest("Số điện thoại không hợp lệ");
            }
            account.FullName = fullName;
            account.Email = email;
            account.Phone = phone;
            appDbContext.SaveChanges();
            HttpContext.Session.SetString("FullName", fullName);
            return Ok("Đã lưu thông tin cá nhân");
        }

        [HttpPut("/profile/password")]
        public IActionResult ChangePassword([FromBody] PasswordChangeDTO dto)
        {
            int accountId = HttpContext.Session.GetInt32("AccountId") ?? 0;
            AppDbContext appDbContext = new AppDbContext();
            var account = appDbContext.Accounts.Where(c => c.Id == accountId).FirstOrDefault();
            if (account == null)
            {
                return NotFound("Không tìm thấy tài khoản");
            }
            string oldPassword = dto.OldPassword ?? "";
            string newPassword = dto.NewPassword ?? "";
            if (account.PasswordHash != PasswordHelper.Hash(oldPassword))
            {
                return BadRequest("Mật khẩu hiện tại không đúng");
            }
            if (newPassword.Length < 6 || newPassword.Length > 50)
            {
                return BadRequest("Mật khẩu mới từ 6 đến 50 ký tự");
            }
            if (newPassword == oldPassword)
            {
                return BadRequest("Mật khẩu mới phải khác mật khẩu hiện tại");
            }
            account.PasswordHash = PasswordHelper.Hash(newPassword);
            appDbContext.SaveChanges();
            return Ok("Đã đổi mật khẩu");
        }

        private string? Clean(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }
            return text.Trim();
        }
    }
}