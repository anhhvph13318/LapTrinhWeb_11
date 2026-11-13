
using LapTrinhWeb_Nhom_11.Models;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace LapTrinhWeb_Nhom_11.Helpers
{
    public class VnPayHelper
    {
        // Thông tin kết nối VNPay
        private const string TMN_CODE = "KKGKUI93";
        private const string HASH_SECRET = "K3V28UR3A2RPUDVE20QGHCZJAZOTUL7Q";

        // Kiểm tra cấu hình VNPay
        public static bool IsConfigured(IConfiguration config)
        {
            return !string.IsNullOrWhiteSpace(TMN_CODE)
                && !string.IsNullOrWhiteSpace(HASH_SECRET);
        }

        // Địa chỉ VNPay chuyển khách về sau khi thanh toán
        public static string GetReturnUrl(IConfiguration config, HttpRequest request)
        {
            string? returnUrl = config["VnPay:ReturnUrl"];

            if (!string.IsNullOrWhiteSpace(returnUrl))
                return returnUrl;

            return request.Scheme + "://" + request.Host
                + "/payment/vnpay-return";
        }

        // Lấy IP của khách hàng
        public static string GetIp(HttpContext context)
        {
            IPAddress? ip = context.Connection.RemoteIpAddress;

            if (ip == null)
                return "127.0.0.1";

            if (ip.IsIPv4MappedToIPv6)
                ip = ip.MapToIPv4();

            if (IPAddress.IsLoopback(ip))
                return "127.0.0.1";

            return ip.ToString();
        }

        // Tạo URL thanh toán VNPay
        public static string CreatePaymentUrl(
            IConfiguration config,
            Order order,
            string returnUrl,
            string ipAddress)
        {
            DateTime now = DateTime.UtcNow.AddHours(7);

            SortedDictionary<string, string> data =
                new SortedDictionary<string, string>(StringComparer.Ordinal);

            data.Add("vnp_Version", "2.1.0");
            data.Add("vnp_Command", "pay");
            data.Add("vnp_TmnCode", TMN_CODE);
            data.Add("vnp_Amount", ((long)(order.TotalAmount * 100)).ToString());
            data.Add("vnp_CreateDate", now.ToString("yyyyMMddHHmmss"));
            data.Add("vnp_CurrCode", "VND");
            data.Add("vnp_IpAddr", ipAddress);
            data.Add("vnp_Locale", "vn");
            data.Add("vnp_OrderInfo", "Thanh toan don hang " + order.OrderCode);
            data.Add("vnp_OrderType", "other");
            data.Add("vnp_ReturnUrl", returnUrl);
            data.Add("vnp_TxnRef", order.OrderCode);
            data.Add("vnp_ExpireDate", now.AddMinutes(15).ToString("yyyyMMddHHmmss"));

            string query = BuildQuery(data);

            // Tạo chữ ký bằng HASH_SECRET
            string hash = Hmac(HASH_SECRET, query);

            string baseUrl = config["VnPay:BaseUrl"]
                ?? "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";

            return baseUrl + "?" + query + "&vnp_SecureHash=" + hash;
        }

        // Xác thực kết quả VNPay trả về
        public static bool Validate(IQueryCollection query)
        {
            // Kiểm tra đúng mã website của mình
            string receivedTmnCode = query["vnp_TmnCode"].ToString();

            if (receivedTmnCode != TMN_CODE)
                return false;

            string receivedHash = query["vnp_SecureHash"].ToString();

            if (string.IsNullOrEmpty(receivedHash))
                return false;

            SortedDictionary<string, string> data =
                new SortedDictionary<string, string>(StringComparer.Ordinal);

            foreach (var item in query)
            {
                if (item.Key.StartsWith("vnp_")
                    && item.Key != "vnp_SecureHash"
                    && item.Key != "vnp_SecureHashType")
                {
                    string value = item.Value.ToString();

                    if (!string.IsNullOrEmpty(value))
                        data[item.Key] = value;
                }
            }

            string hash = Hmac(HASH_SECRET, BuildQuery(data));

            byte[] a = Encoding.UTF8.GetBytes(hash.ToLower());
            byte[] b = Encoding.UTF8.GetBytes(receivedHash.ToLower());

            return a.Length == b.Length
                && CryptographicOperations.FixedTimeEquals(a, b);
        }

        // Ghép dữ liệu theo thứ tự key
        private static string BuildQuery(
            SortedDictionary<string, string> data)
        {
            return string.Join("&",
                data.Select(kv =>
                    WebUtility.UrlEncode(kv.Key) + "="
                    + WebUtility.UrlEncode(kv.Value)));
        }

        // Tạo chữ ký HMAC SHA512
        private static string Hmac(string key, string data)
        {
            using HMACSHA512 hmac =
                new HMACSHA512(Encoding.UTF8.GetBytes(key));

            byte[] bytes = hmac.ComputeHash(
                Encoding.UTF8.GetBytes(data));

            return Convert.ToHexString(bytes).ToLower();
        }
    }
}
