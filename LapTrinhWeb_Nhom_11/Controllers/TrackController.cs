using LapTrinhWeb_Nhom_11.DTO;
using LapTrinhWeb_Nhom_11.Helpers;
using LapTrinhWeb_Nhom_11.Models;
using Microsoft.AspNetCore.Mvc;

namespace LapTrinhWeb_Nhom_11.Controllers
{
    public class TrackController : Controller
    {
        [HttpGet("/track")]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("/track/data")]
        public IActionResult GetData(string? code, string? phone)
        {
            string orderCode = (code ?? "").Trim();
            string orderPhone = (phone ?? "").Trim();
            if (orderCode == "" || orderPhone == "")
            {
                return BadRequest("Vui lòng nhập mã đơn hàng và số điện thoại đặt hàng");
            }
            OrderHelper.ReleaseExpired();
            AppDbContext appDbContext = new AppDbContext();
            var order = appDbContext.Orders.Where(c => c.OrderCode == orderCode && c.Phone == orderPhone).FirstOrDefault();
            if (order == null)
            {
                return NotFound("Không tìm thấy đơn hàng, vui lòng kiểm tra lại mã đơn và số điện thoại");
            }
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
            return Ok(orderDTO);
        }
    }
}