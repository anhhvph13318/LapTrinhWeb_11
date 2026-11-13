namespace LapTrinhWeb_Nhom_11.DTO
{
    // Thông tin tài khoản trả về cho trang admin (không có mật khẩu)
    public class AccountDTO
    {
        public int Id { get; set; }

        public string Username { get; set; } = null!;

        public string? FullName { get; set; }

        public string? Email { get; set; }

        public string? Phone { get; set; }

        public string Role { get; set; } = null!;

        public bool IsActive { get; set; }
    }
}