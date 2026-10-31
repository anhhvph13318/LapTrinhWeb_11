using System.Security.Cryptography;
using System.Text;

namespace LapTrinhWeb_Nhom_11.Helpers
{
    public class PasswordHelper
    {
        // mã hóa mật khẩu bằng SHA256, ra chuỗi 64 ký tự (vừa cột PasswordHash)
        public static string Hash(string password)
        {
            byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
            return Convert.ToHexString(bytes);
        }
    }
}
