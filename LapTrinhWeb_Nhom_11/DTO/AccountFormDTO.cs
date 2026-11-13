namespace LapTrinhWeb_Nhom_11.DTO
{
    // Dữ liệu form tạo / sửa tài khoản
    public class AccountFormDTO
    {
        // chỉ dùng khi tạo mới, khi sửa thì không đổi tên đăng nhập
        public string? Username { get; set; }

        // tạo mới thì bắt buộc, sửa thì để trống nếu không đổi mật khẩu
        public string? Password { get; set; }

        public string? FullName { get; set; }

        public string? Email { get; set; }

        public string? Phone { get; set; }

        // ADMIN hoặc CUSTOMER
        public string Role { get; set; } = null!;
    }
}