using LapTrinhWeb_Nhom_11.DTO;
using LapTrinhWeb_Nhom_11.Helpers;
using LapTrinhWeb_Nhom_11.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

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

        // ================== quản lý tài khoản (chỉ admin) ==================

        // Danh sách tài khoản cho trang admin, có bộ lọc
        [AdminOnly]
        [HttpGet("/accounts/admin")]
        public List<AccountDTO> GetAdmin(string? keyword, string? role, bool? active)
        {
            AppDbContext appDbContext = new AppDbContext();
            var query = appDbContext.Accounts.AsQueryable();
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();
                query = query.Where(c => c.Username.Contains(keyword)
                    || (c.FullName != null && c.FullName.Contains(keyword))
                    || (c.Email != null && c.Email.Contains(keyword))
                    || (c.Phone != null && c.Phone.Contains(keyword)));
            }
            if (!string.IsNullOrWhiteSpace(role))
            {
                query = query.Where(c => c.Role == role);
            }
            if (active != null)
            {
                query = query.Where(c => c.IsActive == active);
            }
            var accounts = query.OrderByDescending(c => c.Id)
                .Select(c => new AccountDTO
                {
                    Id = c.Id,
                    Username = c.Username,
                    FullName = c.FullName,
                    Email = c.Email,
                    Phone = c.Phone,
                    Role = c.Role,
                    IsActive = c.IsActive
                })
                .ToList();
            return accounts;
        }

        // Tạo tài khoản (chọn role ADMIN hoặc CUSTOMER)
        [AdminOnly]
        [HttpPost("/accounts/add")]
        public IActionResult Add([FromBody] AccountFormDTO form)
        {
            AppDbContext appDbContext = new AppDbContext();
            string? error = CheckForm(appDbContext, form, 0);
            if (error != null)
            {
                return BadRequest(error);
            }
            Account account = new Account();
            account.Username = form.Username!.Trim();
            account.PasswordHash = PasswordHelper.Hash(form.Password!);
            account.FullName = Clean(form.FullName);
            account.Email = Clean(form.Email);
            account.Phone = Clean(form.Phone);
            account.Role = form.Role;
            account.IsActive = true;
            appDbContext.Accounts.Add(account);
            appDbContext.SaveChanges();
            return Ok("Tạo tài khoản thành công");
        }

        // Sửa thông tin tài khoản, nhập mật khẩu mới thì đổi mật khẩu
        [AdminOnly]
        [HttpPut("/accounts/update/{id}")]
        public IActionResult Update(int id, [FromBody] AccountFormDTO form)
        {
            AppDbContext appDbContext = new AppDbContext();
            var account = appDbContext.Accounts.Where(c => c.Id == id).FirstOrDefault();
            if (account == null)
            {
                return NotFound("Không tìm thấy tài khoản");
            }
            string? error = CheckForm(appDbContext, form, id);
            if (error != null)
            {
                return BadRequest(error);
            }
            // không cho admin tự đổi vai trò của mình (tránh tự mất quyền admin)
            if (id == HttpContext.Session.GetInt32("AccountId") && form.Role != account.Role)
            {
                return BadRequest("Bạn không thể tự đổi vai trò của chính mình");
            }
            account.FullName = Clean(form.FullName);
            account.Email = Clean(form.Email);
            account.Phone = Clean(form.Phone);
            account.Role = form.Role;
            if (!string.IsNullOrEmpty(form.Password))
            {
                account.PasswordHash = PasswordHelper.Hash(form.Password);
            }
            appDbContext.SaveChanges();
            return Ok("Sửa thành công");
        }

        // Khóa tài khoản (IsActive = false), tài khoản bị khóa không đăng nhập được
        [AdminOnly]
        [HttpPut("/accounts/lock/{id}")]
        public IActionResult Lock(int id)
        {
            if (id == HttpContext.Session.GetInt32("AccountId"))
            {
                return BadRequest("Bạn không thể tự khóa tài khoản của chính mình");
            }
            AppDbContext appDbContext = new AppDbContext();
            var account = appDbContext.Accounts.Where(c => c.Id == id).FirstOrDefault();
            if (account == null)
            {
                return NotFound("Không tìm thấy tài khoản");
            }
            account.IsActive = false;
            appDbContext.SaveChanges();
            return Ok("Đã khóa tài khoản");
        }

        // Mở khóa tài khoản
        [AdminOnly]
        [HttpPut("/accounts/unlock/{id}")]
        public IActionResult Unlock(int id)
        {
            AppDbContext appDbContext = new AppDbContext();
            var account = appDbContext.Accounts.Where(c => c.Id == id).FirstOrDefault();
            if (account == null)
            {
                return NotFound("Không tìm thấy tài khoản");
            }
            account.IsActive = true;
            appDbContext.SaveChanges();
            return Ok("Đã mở khóa tài khoản");
        }

        // ================== các hàm dùng chung ==================

        // chuỗi rỗng thì đổi thành null, còn lại cắt khoảng trắng 2 đầu
        private string? Clean(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }
            return text.Trim();
        }

        // Kiểm tra dữ liệu form, có lỗi thì trả về câu báo lỗi, không lỗi trả về null
        // id = 0 là tạo mới, id > 0 là sửa
        private string? CheckForm(AppDbContext appDbContext, AccountFormDTO form, int id)
        {
            if (id == 0)
            {
                string username = (form.Username ?? "").Trim();
                if (Regex.IsMatch(username, "^[a-zA-Z0-9_.]{3,50}$") == false)
                {
                    return "Tên đăng nhập gồm 3-50 ký tự: chữ không dấu, số, dấu chấm hoặc gạch dưới";
                }
                var sameUsername = appDbContext.Accounts.Where(c => c.Username == username).FirstOrDefault();
                if (sameUsername != null)
                {
                    return "Tên đăng nhập đã tồn tại";
                }
                if (string.IsNullOrEmpty(form.Password))
                {
                    return "Mật khẩu không được để trống";
                }
            }
            if (!string.IsNullOrEmpty(form.Password) && (form.Password.Length < 6 || form.Password.Length > 50))
            {
                return "Mật khẩu từ 6 đến 50 ký tự";
            }
            string? fullName = Clean(form.FullName);
            if (fullName == null)
            {
                return "Họ tên không được để trống";
            }
            if (fullName.Length > 100)
            {
                return "Họ tên tối đa 100 ký tự";
            }
            string? email = Clean(form.Email);
            if (email != null)
            {
                if (email.Length > 100 || Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$") == false)
                {
                    return "Email không hợp lệ";
                }
                var sameEmail = appDbContext.Accounts.Where(c => c.Email == email && c.Id != id).FirstOrDefault();
                if (sameEmail != null)
                {
                    return "Email đã được dùng cho tài khoản khác";
                }
            }
            string? phone = Clean(form.Phone);
            if (phone != null && Regex.IsMatch(phone, @"^\+?[0-9]{9,15}$") == false)
            {
                return "Số điện thoại không hợp lệ";
            }
            if (form.Role != "ADMIN" && form.Role != "CUSTOMER")
            {
                return "Vai trò phải là ADMIN hoặc CUSTOMER";
            }
            return null;
        }
    }
}