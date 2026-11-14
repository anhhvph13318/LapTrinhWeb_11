using LapTrinhWeb_Nhom_11.DTO;
using LapTrinhWeb_Nhom_11.Helpers;
using LapTrinhWeb_Nhom_11.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

namespace LapTrinhWeb_Nhom_11.Controllers
{
    public class AccountController : Controller
    {
        [HttpGet("/login")]
        public IActionResult Login()
        {
            if (HttpContext.Session.GetInt32("AccountId") != null)
            {
                if (HttpContext.Session.GetString("Role") == "ADMIN")
                {
                    return Redirect("/admin");
                }
                if (HttpContext.Session.GetString("Role") == "CUSTOMER")
                {
                    return Redirect("/home");
                }
                return Redirect("/");
            }
            return View();
        }

        [HttpPost("/login")]
        public IActionResult DoLogin([FromBody] LoginDTO dto)
        {
            AppDbContext appDbContext = new AppDbContext();
            string username = (dto.Username ?? "").Trim();
            string passwordHash = PasswordHelper.Hash(dto.Password);
            var account = appDbContext.Accounts
                .Where(c => c.Username == username && c.PasswordHash == passwordHash)
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
            GuestCartHelper.MergeToAccount(HttpContext.Session, appDbContext, account.Id);

            string redirect = "/";
            if (account.Role == "ADMIN")
            {
                redirect = "/admin";
            }
            return Ok(new { redirect = redirect });
        }

        [HttpGet("/logout")]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return Redirect("/login");
        }

        [HttpPost("/register")]
        public IActionResult Register([FromBody] AccountFormDTO form)
        {
            form.Role = "CUSTOMER";
            AppDbContext appDbContext = new AppDbContext();
            string? error = CheckForm(appDbContext, form, 0);
            if (error != null)
            {
                return BadRequest(error);
            }
            CreateAccount(appDbContext, form, "CUSTOMER");
            return Ok("Đăng ký thành công");
        }

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
            CreateAccount(appDbContext, form, form.Role);
            return Ok("Tạo tài khoản thành công");
        }

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

        private void CreateAccount(AppDbContext appDbContext, AccountFormDTO form, string role)
        {
            Account account = new Account();
            account.Username = form.Username!.Trim();
            account.PasswordHash = PasswordHelper.Hash(form.Password!);
            account.FullName = Clean(form.FullName);
            account.Email = Clean(form.Email);
            account.Phone = Clean(form.Phone);
            account.Role = role;
            account.IsActive = true;
            appDbContext.Accounts.Add(account);
            appDbContext.SaveChanges();
        }

        private string? Clean(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }
            return text.Trim();
        }

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