using LapTrinhWeb_Nhom_11.Models;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace LapTrinhWeb_Nhom_11.Helpers
{
    public class VnPayHelper
    {
        // Đã điền VnPay:TmnCode và VnPay:HashSecret chưa
        public static bool IsConfigured(IConfiguration config)
        {
            return !string.IsNullOrWhiteSpace(config["VnPay:TmnCode"]) && !string.IsNullOrWhiteSpace(config["VnPay:HashSecret"]);
        }

        // Địa chỉ VNPay chuyển khách về sau khi thanh toán
        // Không điền VnPay:ReturnUrl thì tự lấy theo địa chỉ web đang chạy
        public static string GetReturnUrl(IConfiguration config, HttpRequest request)
        {
            string? returnUrl = config["VnPay:ReturnUrl"];
            if (!string.IsNullOrWhiteSpace(returnUrl))
            {
                return returnUrl;
            }
            return request.Scheme + "://" + request.Host + "/payment/vnpay-return";
        }

        // IP của khách (VNPay yêu cầu có trường này)
        public static string GetIp(HttpContext context)
        {
            IPAddress? ip = context.Connection.RemoteIpAddress;
            if (ip == null)
            {
                return "127.0.0.1";
            }
            if (ip.IsIPv4MappedToIPv6)
            {
                ip = ip.MapToIPv4();
            }
            if (IPAddress.IsLoopback(ip))
            {
                return "127.0.0.1";
            }
            return ip.ToString();
        }

        // Tạo link sang trang thanh toán VNPay
        public static string CreatePaymentUrl(IConfiguration config, Order order, string returnUrl, string ipAddress)
        {
            // VNPay dùng giờ Việt Nam (UTC+7), tự cộng 7 tiếng để chạy trên máy chủ nào cũng đúng
            DateTime now = DateTime.UtcNow.AddHours(7);
            SortedDictionary<string, string> data = new SortedDictionary<string, string>(StringComparer.Ordinal);
            data.Add("vnp_Version", "2.1.0");
            data.Add("vnp_Command", "pay");
            data.Add("vnp_TmnCode", config["VnPay:TmnCode"]!);
            data.Add("vnp_Amount", ((long)(order.TotalAmount * 100)).ToString()); // VNPay yêu cầu nhân 100
            data.Add("vnp_CreateDate", now.ToString("yyyyMMddHHmmss"));
            data.Add("vnp_CurrCode", "VND");
            data.Add("vnp_IpAddr", ipAddress);
            data.Add("vnp_Locale", "vn");
            data.Add("vnp_OrderInfo", "Thanh toan don hang " + order.OrderCode); // không dấu, không ký tự đặc biệt
            data.Add("vnp_OrderType", "other");
            data.Add("vnp_ReturnUrl", returnUrl);
            data.Add("vnp_TxnRef", order.OrderCode);
            data.Add("vnp_ExpireDate", now.AddMinutes(15).ToString("yyyyMMddHHmmss"));

            string query = BuildQuery(data);
            string hash = Hmac(config["VnPay:HashSecret"]!, query);
            string baseUrl = config["VnPay:BaseUrl"] ?? "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
            return baseUrl + "?" + query + "&vnp_SecureHash=" + hash;
        }

        // Kiểm tra chữ ký VNPay gửi về (cả khi khách quay lại web lẫn khi VNPay gọi IPN)
        public static bool Validate(IQueryCollection query, string hashSecret)
        {
            string receivedHash = query["vnp_SecureHash"].ToString();
            if (string.IsNullOrEmpty(receivedHash) || string.IsNullOrEmpty(hashSecret))
            {
                return false;
            }
            SortedDictionary<string, string> data = new SortedDictionary<string, string>(StringComparer.Ordinal);
            foreach (var item in query)
            {
                // bỏ vnp_SecureHash và vnp_SecureHashType, chỉ lấy các trường vnp_ có giá trị
                if (item.Key.StartsWith("vnp_") && item.Key != "vnp_SecureHash" && item.Key != "vnp_SecureHashType")
                {
                    string value = item.Value.ToString();
                    if (!string.IsNullOrEmpty(value))
                    {
                        data[item.Key] = value;
                    }
                }
            }
            string hash = Hmac(hashSecret, BuildQuery(data));
            // so sánh theo thời gian cố định để tránh bị dò chữ ký
            byte[] a = Encoding.UTF8.GetBytes(hash.ToLower());
            byte[] b = Encoding.UTF8.GetBytes(receivedHash.ToLower());
            return CryptographicOperations.FixedTimeEquals(a, b);
        }

        // Ghép thành chuỗi key=value&key=value (key đã sắp xếp, value được mã hóa URL)
        // Chữ ký được tính trên đúng chuỗi này
        private static string BuildQuery(SortedDictionary<string, string> data)
        {
            return string.Join("&", data.Select(kv => WebUtility.UrlEncode(kv.Key) + "=" + WebUtility.UrlEncode(kv.Value)));
        }

        // HMAC SHA512, ra chuỗi hex chữ thường
        private static string Hmac(string key, string data)
        {
            using HMACSHA512 hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key));
            byte[] bytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToHexString(bytes).ToLower();
        }
    }
}