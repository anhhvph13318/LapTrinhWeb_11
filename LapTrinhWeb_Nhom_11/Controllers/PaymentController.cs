using LapTrinhWeb_Nhom_11.Helpers;
using LapTrinhWeb_Nhom_11.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LapTrinhWeb_Nhom_11.Controllers
{
    [Route("/payment")]
    public class PaymentController : Controller
    {
        private readonly IConfiguration _config;

        public PaymentController(IConfiguration config)
        {
            _config = config;
        }

        [HttpGet("vnpay-ipn")]
        public IActionResult VnPayIpn()
        {
            PaymentResult result = Confirm();
            return Content("{\"RspCode\":\"" + result.RspCode + "\",\"Message\":\"" + result.Message + "\"}", "application/json");
        }

        [HttpGet("vnpay-return")]
        public IActionResult VnPayReturn()
        {
            PaymentResult result = Confirm();
            ViewBag.Success = result.Success;
            ViewBag.OrderCode = result.OrderCode;
            ViewBag.Message = result.UserMessage;
            return View("Result");
        }

        private PaymentResult Confirm()
        {
            PaymentResult result = new PaymentResult();
            string hashSecret = _config["VnPay:HashSecret"] ?? "";
            if (VnPayHelper.Validate(Request.Query) == false)
            {
                result.RspCode = "97";
                result.Message = "Invalid signature";
                result.UserMessage = "Dữ liệu thanh toán không hợp lệ";
                return result;
            }

            string txnRef = Request.Query["vnp_TxnRef"].ToString();
            string responseCode = Request.Query["vnp_ResponseCode"].ToString();
            string transactionStatus = Request.Query["vnp_TransactionStatus"].ToString();
            long vnpAmount;
            long.TryParse(Request.Query["vnp_Amount"].ToString(), out vnpAmount);

            AppDbContext appDbContext = new AppDbContext();
            var order = appDbContext.Orders.Where(c => c.OrderCode == txnRef && c.PaymentMethod == 1).FirstOrDefault();
            if (order == null)
            {
                result.RspCode = "01";
                result.Message = "Order not found";
                result.UserMessage = "Không tìm thấy đơn hàng";
                return result;
            }
            result.OrderCode = order.OrderCode;
            if (order.TotalAmount * 100 != vnpAmount)
            {
                result.RspCode = "04";
                result.Message = "Invalid amount";
                result.UserMessage = "Số tiền thanh toán không khớp với đơn hàng";
                return result;
            }
            if (order.PaymentStatus == 1)
            {
                result.RspCode = "02";
                result.Message = "Order already confirmed";
                result.Success = true;
                result.UserMessage = "Đơn hàng này đã được thanh toán trước đó";
                return result;
            }

            bool paid = responseCode == "00" && transactionStatus == "00";
            using var tran = appDbContext.Database.BeginTransaction();
            if (paid)
            {
                int affected = appDbContext.Orders
                    .Where(c => c.Id == order.Id && c.PaymentStatus == 0)
                    .ExecuteUpdate(s => s.SetProperty(c => c.PaymentStatus, 1));
                if (affected == 0)
                {
                    result.RspCode = "02";
                    result.Message = "Order already confirmed";
                    result.Success = true;
                    result.UserMessage = "Đơn hàng này đã được thanh toán trước đó";
                    return result;
                }
                if (order.Status != 4 && order.AccountId != null)
                {
                    int? cartId = appDbContext.Carts.Where(c => c.AccountId == order.AccountId).Select(c => (int?)c.Id).FirstOrDefault();
                    var detailIds = appDbContext.OrderDetails.Where(c => c.OrderId == order.Id).Select(c => c.ProductDetailId).ToList();
                    if (cartId != null)
                    {
                        appDbContext.CartItems.Where(c => c.CartId == cartId && detailIds.Contains(c.ProductDetailId)).ExecuteDelete();
                    }
                }
                tran.Commit();
                result.RspCode = "00";
                result.Message = "Confirm Success";
                result.Success = true;
                if (order.Status == 4)
                {
                    result.UserMessage = "Thanh toán thành công nhưng đơn hàng đã bị hủy, cửa hàng sẽ liên hệ để hoàn tiền cho bạn";
                }
                else
                {
                    result.UserMessage = "Thanh toán thành công, cảm ơn bạn đã mua hàng";
                }
                return result;
            }

            OrderHelper.CancelAndRestock(appDbContext, order.Id, 0);
            tran.Commit();
            result.RspCode = "00";
            result.Message = "Confirm Success";
            result.Success = false;
            result.UserMessage = "Thanh toán không thành công hoặc đã bị hủy. Đơn hàng đã được hủy, giỏ hàng của bạn vẫn còn để đặt lại";
            return result;
        }

        private class PaymentResult
        {
            public string RspCode { get; set; } = "99";

            public string Message { get; set; } = "Unknown error";

            public bool Success { get; set; }

            public string? OrderCode { get; set; }

            public string UserMessage { get; set; } = "";
        }
    }
}