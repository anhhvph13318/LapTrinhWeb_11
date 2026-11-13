using LapTrinhWeb_Nhom_11.DTO;
using LapTrinhWeb_Nhom_11.Helpers;
using LapTrinhWeb_Nhom_11.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LapTrinhWeb_Nhom_11.Controllers
{
    // Giỏ hàng của người đang đăng nhập (mỗi tài khoản 1 giỏ)
    [LoginRequired]
    [Route("/cart")]
    public class CartController : Controller
    {
        // Xem giỏ hàng
        [HttpGet("")]
        public CartDTO Get()
        {
            AppDbContext appDbContext = new AppDbContext();
            var cart = GetCart(appDbContext);
            var items = appDbContext.CartItems.Where(c => c.CartId == cart.Id)
                .OrderByDescending(c => c.Id)
                .Select(c => new CartItemDTO
                {
                    Id = c.Id,
                    ProductDetailId = c.ProductDetailId,
                    ProductId = c.ProductDetail.ProductId,
                    ProductCode = c.ProductDetail.Product.Code,
                    ProductName = c.ProductDetail.Product.Name,
                    ImageUrl = c.ProductDetail.Product.ImageUrl,
                    SizeName = c.ProductDetail.Size.Name,
                    ColorName = c.ProductDetail.Color.Name,
                    Price = c.ProductDetail.Product.Price,
                    Quantity = c.Quantity,
                    Stock = c.ProductDetail.Quantity,
                    IsAvailable = c.ProductDetail.Product.IsDeleted == false
                        && c.ProductDetail.Product.Status == 1
                        && c.ProductDetail.Quantity >= c.Quantity
                })
                .ToList();
            // ảnh lấy từ bảng Image (ưu tiên ảnh chính), có ảnh thì dùng thay cho cột ImageUrl của sản phẩm
            var imageUrls = ImageHelper.GetMainImageUrls(appDbContext, items.Select(c => c.ProductId).Distinct().ToList());
            foreach (var item in items)
            {
                if (imageUrls.ContainsKey(item.ProductId))
                {
                    item.ImageUrl = imageUrls[item.ProductId];
                }
            }
            CartDTO cartDTO = new CartDTO();
            cartDTO.Items = items;
            cartDTO.TotalQuantity = items.Sum(c => c.Quantity);
            cartDTO.TotalAmount = items.Sum(c => c.Price * c.Quantity);
            return cartDTO;
        }

        // Số lượng sản phẩm trong giỏ (để hiện trên icon giỏ hàng)
        [HttpGet("count")]
        public IActionResult Count()
        {
            AppDbContext appDbContext = new AppDbContext();
            var cart = GetCart(appDbContext);
            int count = appDbContext.CartItems.Where(c => c.CartId == cart.Id).Sum(c => (int?)c.Quantity) ?? 0;
            return Ok(count);
        }

        // Thêm vào giỏ, đã có biến thể này trong giỏ thì cộng dồn số lượng
        [HttpPost("add")]
        public IActionResult Add([FromBody] CartAddDTO dto)
        {
            if (dto.Quantity < 1 || dto.Quantity > 1000)
            {
                return BadRequest("Số lượng phải từ 1 đến 1000");
            }
            AppDbContext appDbContext = new AppDbContext();
            var detail = appDbContext.ProductDetails.Where(c => c.Id == dto.ProductDetailId)
                .Select(c => new { c.Id, c.Quantity, c.Product.IsDeleted, c.Product.Status })
                .FirstOrDefault();
            if (detail == null || detail.IsDeleted || detail.Status != 1)
            {
                return BadRequest("Sản phẩm không tồn tại hoặc đã ngừng bán");
            }
            var cart = GetCart(appDbContext);
            var item = appDbContext.CartItems.Where(c => c.CartId == cart.Id && c.ProductDetailId == dto.ProductDetailId).FirstOrDefault();
            int inCart = item == null ? 0 : item.Quantity;
            if (dto.Quantity + inCart > detail.Quantity)
            {
                return BadRequest("Kho chỉ còn " + detail.Quantity + " sản phẩm (trong giỏ của bạn đã có " + inCart + ")");
            }
            if (item == null)
            {
                item = new CartItem();
                item.CartId = cart.Id;
                item.ProductDetailId = dto.ProductDetailId;
                item.Quantity = dto.Quantity;
                item.CreatedAt = DateTime.Now;
                appDbContext.CartItems.Add(item);
            }
            else
            {
                item.Quantity = item.Quantity + dto.Quantity;
            }
            cart.UpdatedAt = DateTime.Now;
            appDbContext.SaveChanges();
            int count = appDbContext.CartItems.Where(c => c.CartId == cart.Id).Sum(c => (int?)c.Quantity) ?? 0;
            return Ok(new { message = "Đã thêm vào giỏ hàng", totalQuantity = count });
        }

        // Sửa số lượng của 1 dòng trong giỏ
        [HttpPut("update/{id}")]
        public IActionResult Update(int id, [FromBody] CartUpdateDTO dto)
        {
            if (dto.Quantity < 1 || dto.Quantity > 1000)
            {
                return BadRequest("Số lượng phải từ 1 đến 1000");
            }
            AppDbContext appDbContext = new AppDbContext();
            var cart = GetCart(appDbContext);
            var item = appDbContext.CartItems.Where(c => c.Id == id && c.CartId == cart.Id).FirstOrDefault();
            if (item == null)
            {
                return NotFound("Không tìm thấy sản phẩm trong giỏ hàng");
            }
            int stock = appDbContext.ProductDetails.Where(c => c.Id == item.ProductDetailId).Select(c => c.Quantity).FirstOrDefault();
            if (dto.Quantity > stock)
            {
                return BadRequest("Kho chỉ còn " + stock + " sản phẩm");
            }
            item.Quantity = dto.Quantity;
            cart.UpdatedAt = DateTime.Now;
            appDbContext.SaveChanges();
            return Ok("Đã cập nhật số lượng");
        }

        // Xóa 1 dòng khỏi giỏ
        [HttpDelete("remove/{id}")]
        public IActionResult Remove(int id)
        {
            AppDbContext appDbContext = new AppDbContext();
            var cart = GetCart(appDbContext);
            var item = appDbContext.CartItems.Where(c => c.Id == id && c.CartId == cart.Id).FirstOrDefault();
            if (item == null)
            {
                return NotFound("Không tìm thấy sản phẩm trong giỏ hàng");
            }
            appDbContext.CartItems.Remove(item);
            cart.UpdatedAt = DateTime.Now;
            appDbContext.SaveChanges();
            return Ok("Đã xóa khỏi giỏ hàng");
        }

        // Xóa hết giỏ hàng
        [HttpDelete("clear")]
        public IActionResult Clear()
        {
            AppDbContext appDbContext = new AppDbContext();
            var cart = GetCart(appDbContext);
            appDbContext.CartItems.Where(c => c.CartId == cart.Id).ExecuteDelete();
            return Ok("Đã xóa hết giỏ hàng");
        }

        // Lấy giỏ hàng của người đang đăng nhập, chưa có thì tạo mới
        private Cart GetCart(AppDbContext appDbContext)
        {
            int accountId = HttpContext.Session.GetInt32("AccountId") ?? 0;
            var cart = appDbContext.Carts.Where(c => c.AccountId == accountId).FirstOrDefault();
            if (cart == null)
            {
                cart = new Cart();
                cart.AccountId = accountId;
                cart.CreatedAt = DateTime.Now;
                cart.UpdatedAt = DateTime.Now;
                appDbContext.Carts.Add(cart);
                appDbContext.SaveChanges();
            }
            return cart;
        }
    }
}