using LapTrinhWeb_Nhom_11.DTO;
using LapTrinhWeb_Nhom_11.Models;
using Microsoft.AspNetCore.Mvc;

namespace LapTrinhWeb_Nhom_11.Controllers
{
    [Route("/ProductDetail")]
    public class ProductDetailController : Controller
    {
        [HttpGet("")]
        public IActionResult Index()
        {
            return View();
        }
        [HttpGet("{id}")]
        public ProductDetailDTO GetProduct(int id)

        {
            AppDbContext dbContext = new AppDbContext();
            var product = dbContext.Products.FirstOrDefault(x => x.Id == id);
            var productDetail = new ProductDetailDTO();
            productDetail.Id = product.Id;
            productDetail.Name = product.Name;
            foreach (var item in dbContext.ProductDetails.Where(c => c.ProductId == product.Id).ToList())
            {
                var size = dbContext.Sizes.Where(c => c.Id == item.SizeId).Select(c => c.Name).FirstOrDefault();
                productDetail.Sizes.Add(size);
                var color = dbContext.Colors.Where(c => c.Id == item.ColorId).Select(c => c.Name).FirstOrDefault();
                productDetail.Colors.Add(color);
            }

            // Loại bỏ các phần tử trùng nhau
            productDetail.Sizes = productDetail.Sizes.Distinct().ToList();
            productDetail.Colors = productDetail.Colors.Distinct().ToList();
            return productDetail;
        }
    }
}
