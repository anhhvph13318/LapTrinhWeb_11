using LapTrinhWeb_Nhom_11.DTO;
using LapTrinhWeb_Nhom_11.Helpers;
using LapTrinhWeb_Nhom_11.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace LapTrinhWeb_Nhom_11.Controllers
{
    [Route("/orders")]
    public class OrderController : Controller
    {
        private readonly IConfiguration _config;

        public OrderController(IConfiguration config)
        {
            _config = config;
        }


        [LoginRequired]
        [HttpPost("create")]
        public IActionResult Create([FromBody] CheckoutDTO dto)
        {
            string? error = CheckCheckout(dto);
            if (error != null)
            {
                return BadRequest(error);
            }
            if (dto.PaymentMethod == 1 && VnPayHelper.IsConfigured(_config) == false)
            {
                return BadRequest("Chưa cấu hình VNPay (cần điền VnPay:TmnCode và VnPay:HashSecret)");
            }

            int accountId = HttpContext.Session.GetInt32("AccountId") ?? 0;
            OrderHelper.ReleaseExpired(); // trả lại kho các đơn VNPay quá hạn trước khi kiểm tra tồn kho

            AppDbContext appDbContext = new AppDbContext();
            using var tran = appDbContext.Database.BeginTransaction();
            try
            {
                var cart = appDbContext.Carts.Where(c => c.AccountId == accountId).FirstOrDefault();
                if (cart == null)
                {
                    return BadRequest("Giỏ hàng trống");
                }
                var cartItems = appDbContext.CartItems.Where(c => c.CartId == cart.Id).ToList();
                if (dto.CartItemIds.Count > 0)
                {
                    var ids = dto.CartItemIds.Distinct().ToList();
                    cartItems = cartItems.Where(c => ids.Contains(c.Id)).ToList();
                    if (cartItems.Count != ids.Count)
                    {
                        return BadRequest("Có sản phẩm không còn trong giỏ hàng, vui lòng tải lại giỏ hàng");
                    }
                }
                if (cartItems.Count == 0)
                {
                    return BadRequest("Giỏ hàng trống");
                }

                Order order = new Order();
                decimal total = 0;
                foreach (var item in cartItems)
                {
                    var detail = appDbContext.ProductDetails
                        .Include(c => c.Product)
                        .Include(c => c.Size)
                        .Include(c => c.Color)
                        .Where(c => c.Id == item.ProductDetailId)
                        .FirstOrDefault();
                    if (detail == null || detail.Product.IsDeleted || detail.Product.Status != 1)
                    {
                        string productName = detail == null ? "Một sản phẩm" : detail.Product.Name;
                        return BadRequest(productName + " không còn được bán, vui lòng xóa khỏi giỏ hàng");
                    }

                    int detailId = detail.Id;
                    int quantity = item.Quantity;
                    int affected = appDbContext.ProductDetails
                        .Where(c => c.Id == detailId && c.Quantity >= quantity)
                        .ExecuteUpdate(s => s.SetProperty(c => c.Quantity, c => c.Quantity - quantity));
                    if (affected == 0)
                    {
                        return BadRequest(detail.Product.Name + " (" + detail.Size.Name + " / " + detail.Color.Name + ") không đủ số lượng trong kho");
                    }

                    OrderDetail orderDetail = new OrderDetail();
                    orderDetail.ProductDetailId = detailId;
                    orderDetail.Quantity = quantity;
                    orderDetail.Price = detail.Product.Price;
                    order.OrderDetails.Add(orderDetail);
                    total = total + detail.Product.Price * quantity;
                }

                order.OrderCode = NewOrderCode(appDbContext);
                order.CustomerName = dto.CustomerName!.Trim();
                order.Phone = dto.Phone!.Trim();
                order.Address = dto.Address!.Trim();
                order.Note = string.IsNullOrWhiteSpace(dto.Note) ? null : dto.Note.Trim();
                order.TotalAmount = total;
                order.PaymentMethod = dto.PaymentMethod;
                order.PaymentStatus = 0;
                order.Status = 0;
                order.CreatedAt = DateTime.Now;
                order.AccountId = accountId;
                appDbContext.Orders.Add(order);

                if (dto.PaymentMethod == 0)
                {
                    appDbContext.CartItems.RemoveRange(cartItems);
                }
                cart.UpdatedAt = DateTime.Now;
                appDbContext.SaveChanges();
                tran.Commit();

                if (dto.PaymentMethod == 1)
                {
                    string returnUrl = VnPayHelper.GetReturnUrl(_config, Request);
                    string paymentUrl = VnPayHelper.CreatePaymentUrl(_config, order, returnUrl, VnPayHelper.GetIp(HttpContext));
                    return Ok(new
                    {
                        message = "Tạo đơn thành công, chuyển sang VNPay để thanh toán trong 15 phút",
                        orderId = order.Id,
                        orderCode = order.OrderCode,
                        paymentUrl = paymentUrl
                    });
                }
                return Ok(new
                {
                    message = "Đặt hàng thành công",
                    orderId = order.Id,
                    orderCode = order.OrderCode,
                    paymentUrl = (string?)null
                });
            }
            catch (Exception ex)
            {
                return BadRequest("Đặt hàng thất bại: " + (ex.InnerException?.Message ?? ex.Message));
            }
        }

        [LoginRequired]
        [HttpGet("my")]
        public List<OrderDTO> GetMy()
        {
            OrderHelper.ReleaseExpired();
            int accountId = HttpContext.Session.GetInt32("AccountId") ?? 0;
            AppDbContext appDbContext = new AppDbContext();
            var query = appDbContext.Orders.Where(c => c.AccountId == accountId).OrderByDescending(c => c.CreatedAt);
            return ToDTOs(query);
        }

        [LoginRequired]
        [HttpGet("my/{id}")]
        public IActionResult GetMyDetail(int id)
        {
            int accountId = HttpContext.Session.GetInt32("AccountId") ?? 0;
            AppDbContext appDbContext = new AppDbContext();
            var order = appDbContext.Orders.Where(c => c.Id == id && c.AccountId == accountId).FirstOrDefault();
            if (order == null)
            {
                return NotFound("Không tìm thấy hóa đơn");
            }
            return Ok(BuildDetail(appDbContext, order));
        }

        [LoginRequired]
        [HttpPut("cancel/{id}")]
        public IActionResult CancelMy(int id)
        {
            int accountId = HttpContext.Session.GetInt32("AccountId") ?? 0;
            AppDbContext appDbContext = new AppDbContext();
            var order = appDbContext.Orders.Where(c => c.Id == id && c.AccountId == accountId).FirstOrDefault();
            if (order == null)
            {
                return NotFound("Không tìm thấy hóa đơn");
            }
            if (order.Status != 0)
            {
                return BadRequest("Chỉ hủy được đơn đang chờ xác nhận");
            }
            if (order.PaymentMethod == 1 && order.PaymentStatus == 1)
            {
                return BadRequest("Đơn đã thanh toán qua VNPay, vui lòng liên hệ cửa hàng để hủy và hoàn tiền");
            }
            using var tran = appDbContext.Database.BeginTransaction();
            bool ok = OrderHelper.CancelAndRestock(appDbContext, id, 0);
            tran.Commit();
            if (ok == false)
            {
                return BadRequest("Không thể hủy đơn này, đơn vừa được cập nhật trạng thái");
            }
            return Ok("Đã hủy đơn hàng");
        }


        [AdminOnly]
        [HttpGet("admin")]
        public List<OrderDTO> GetAdmin(string? keyword, int? status, DateTime? fromDate, DateTime? toDate)
        {
            OrderHelper.ReleaseExpired();
            AppDbContext appDbContext = new AppDbContext();
            var query = appDbContext.Orders.AsQueryable();
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();
                query = query.Where(c => c.OrderCode.Contains(keyword) || c.CustomerName.Contains(keyword) || c.Phone.Contains(keyword));
            }
            if (status != null)
            {
                query = query.Where(c => c.Status == status);
            }
            if (fromDate != null)
            {
                DateTime from = fromDate.Value.Date;
                query = query.Where(c => c.CreatedAt >= from);
            }
            if (toDate != null)
            {
                DateTime to = toDate.Value.Date.AddDays(1);
                query = query.Where(c => c.CreatedAt < to);
            }
            return ToDTOs(query.OrderByDescending(c => c.CreatedAt));
        }

        [AdminOnly]
        [HttpGet("admin/{id}")]
        public IActionResult GetDetail(int id)
        {
            AppDbContext appDbContext = new AppDbContext();
            var order = appDbContext.Orders.Where(c => c.Id == id).FirstOrDefault();
            if (order == null)
            {
                return NotFound("Không tìm thấy hóa đơn");
            }
            return Ok(BuildDetail(appDbContext, order));
        }

        [AdminOnly]
        [HttpPut("status/{id}")]
        public IActionResult UpdateStatus(int id, [FromBody] OrderStatusDTO dto)
        {
            AppDbContext appDbContext = new AppDbContext();
            var order = appDbContext.Orders.Where(c => c.Id == id).FirstOrDefault();
            if (order == null)
            {
                return NotFound("Không tìm thấy hóa đơn");
            }
            if (CanChange(order.Status, dto.Status) == false)
            {
                return BadRequest("Không thể chuyển hóa đơn sang trạng thái này");
            }
            if (order.PaymentMethod == 1 && order.PaymentStatus == 0 && dto.Status != 4)
            {
                return BadRequest("Đơn VNPay chưa thanh toán nên chưa thể chuyển trạng thái");
            }

            if (dto.Status == 4)
            {
                using var tran = appDbContext.Database.BeginTransaction();
                bool ok = OrderHelper.CancelAndRestock(appDbContext, id, 2);
                tran.Commit();
                if (ok == false)
                {
                    return BadRequest("Không thể hủy đơn này, đơn vừa được cập nhật trạng thái");
                }
                string note = "";
                if (order.PaymentMethod == 1 && order.PaymentStatus == 1)
                {
                    note = ". Lưu ý: đơn đã thanh toán qua VNPay, cần hoàn tiền thủ công trên trang quản trị VNPay";
                }
                return Ok("Đã hủy đơn và cộng lại số lượng vào kho" + note);
            }

            order.Status = dto.Status;
            if (dto.Status == 3 && order.PaymentMethod == 0)
            {
                order.PaymentStatus = 1;
            }
            appDbContext.SaveChanges();
            return Ok("Đã cập nhật trạng thái");
        }


        private List<OrderDTO> ToDTOs(IQueryable<Order> query)
        {
            return query.Select(c => new OrderDTO
            {
                Id = c.Id,
                OrderCode = c.OrderCode,
                CustomerName = c.CustomerName,
                Phone = c.Phone,
                Address = c.Address,
                Note = c.Note,
                TotalAmount = c.TotalAmount,
                PaymentMethod = c.PaymentMethod,
                PaymentStatus = c.PaymentStatus,
                Status = c.Status,
                CreatedAt = c.CreatedAt,
                AccountId = c.AccountId,
                TrackingCode = c.TrackingCode,
                ItemCount = c.OrderDetails.Sum(d => d.Quantity)
            }).ToList();
        }

        private OrderDTO BuildDetail(AppDbContext appDbContext, Order order)
        {
            OrderDTO orderDTO = new OrderDTO();
            orderDTO.Id = order.Id;
            orderDTO.OrderCode = order.OrderCode;
            orderDTO.CustomerName = order.CustomerName;
            orderDTO.Phone = order.Phone;
            orderDTO.Address = order.Address;
            orderDTO.Note = order.Note;
            orderDTO.TotalAmount = order.TotalAmount;
            orderDTO.PaymentMethod = order.PaymentMethod;
            orderDTO.PaymentStatus = order.PaymentStatus;
            orderDTO.Status = order.Status;
            orderDTO.CreatedAt = order.CreatedAt;
            orderDTO.AccountId = order.AccountId;
            orderDTO.TrackingCode = order.TrackingCode;

            var items = appDbContext.OrderDetails.Where(d => d.OrderId == order.Id)
                .Select(d => new OrderItemDTO
                {
                    Id = d.Id,
                    ProductDetailId = d.ProductDetailId,
                    ProductId = d.ProductDetail.ProductId,
                    ProductCode = d.ProductDetail.Product.Code,
                    ProductName = d.ProductDetail.Product.Name,
                    ImageUrl = d.ProductDetail.Product.ImageUrl,
                    SizeName = d.ProductDetail.Size.Name,
                    ColorName = d.ProductDetail.Color.Name,
                    Quantity = d.Quantity,
                    Price = d.Price
                })
                .ToList();
            var imageUrls = ImageHelper.GetMainImageUrls(appDbContext, items.Select(d => d.ProductId).Distinct().ToList());
            foreach (var item in items)
            {
                if (imageUrls.ContainsKey(item.ProductId))
                {
                    item.ImageUrl = imageUrls[item.ProductId];
                }
            }
            orderDTO.Items = items;
            orderDTO.ItemCount = items.Sum(d => d.Quantity);
            return orderDTO;
        }

        private string? CheckCheckout(CheckoutDTO dto)
        {
            string name = (dto.CustomerName ?? "").Trim();
            if (name == "")
            {
                return "Họ tên người nhận không được để trống";
            }
            if (name.Length > 100)
            {
                return "Họ tên tối đa 100 ký tự";
            }
            string phone = (dto.Phone ?? "").Trim();
            if (Regex.IsMatch(phone, @"^\+?[0-9]{9,15}$") == false)
            {
                return "Số điện thoại không hợp lệ";
            }
            string address = (dto.Address ?? "").Trim();
            if (address == "")
            {
                return "Địa chỉ giao hàng không được để trống";
            }
            if (address.Length > 300)
            {
                return "Địa chỉ tối đa 300 ký tự";
            }
            if (dto.Note != null && dto.Note.Trim().Length > 500)
            {
                return "Ghi chú tối đa 500 ký tự";
            }
            if (dto.PaymentMethod != 0 && dto.PaymentMethod != 1)
            {
                return "Phương thức thanh toán phải là 0 (COD) hoặc 1 (VNPay)";
            }
            return null;
        }

        private string NewOrderCode(AppDbContext appDbContext)
        {
            string code;
            do
            {
                code = "DH" + DateTime.Now.ToString("yyMMddHHmmss") + Random.Shared.Next(100, 1000);
            }
            while (appDbContext.Orders.Any(c => c.OrderCode == code));
            return code;
        }

        private bool CanChange(int from, int to)
        {
            if (from == 0)
            {
                return to == 1 || to == 4;
            }
            if (from == 1)
            {
                return to == 2 || to == 4;
            }
            if (from == 2)
            {
                return to == 3 || to == 4;
            }
            return false;
        }
    }
}