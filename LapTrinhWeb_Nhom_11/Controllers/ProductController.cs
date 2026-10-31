using LapTrinhWeb_Nhom_11.DTO;
using LapTrinhWeb_Nhom_11.Models;
using Microsoft.AspNetCore.Mvc;

namespace LapTrinhWeb_Nhom_11.Controllers
{
    [Route("/products")]
    public class ProductController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        [HttpGet]
        public List<ProductDTO> GetAll()
        {
            AppDbContext appDbContext = new AppDbContext();
            ProductDetailDTO productDetailDTO = new ProductDetailDTO();
            ImageDTO imageDTO = new ImageDTO();
            ProductDTO productDTO = new ProductDTO();
            var product = appDbContext.Products.ToList();
            List<ProductDTO> productDTOs = product.Select(p => new ProductDTO
            {
                Id = p.Id,
                Name = p.Name,
                Price = p.Price,
                Code = p.Code,
                CategoryId = p.CategoryId,
                CreatedAt = p.CreatedAt,
                Description = p.Description,
                ImageUrl = p.ImageUrl,
                IsDeleted = p.IsDeleted,
                Status = p.Status,
                ProductDetails = new(),
                Images = new()
            }).ToList();
            foreach (var item in productDTOs)
            {
                var productdetails = appDbContext.ProductDetails.Where(c => c.ProductId == item.Id).ToList();
                foreach (var item1 in productdetails)
                {
                    productDetailDTO.Id = item1.Id;
                    productDetailDTO.ColorId = item1.ColorId;
                    productDetailDTO.SizeId = item1.SizeId;
                    productDetailDTO.Quantity = item1.Quantity;
                    productDetailDTO.ProductId = item1.ProductId;
                    item.ProductDetails.Add(productDetailDTO);
                }
                var images = appDbContext.Images.Where(c => c.ProductId == item.Id).ToList();
                foreach (var item1 in images)
                {
                    imageDTO.Id = item1.Id;
                    imageDTO.ProductId = item1.ProductId;
                    imageDTO.FileName = item1.FileName;
                    imageDTO.ContentType = item1.ContentType;
                    imageDTO.ImageUrl = "images/" + imageDTO.Id;
                    imageDTO.IsMain = item1.IsMain;
                    imageDTO.SortOrder = item1.SortOrder;
                    imageDTO.Version = item1.Version;
                    imageDTO.CreatedAt = item1.CreatedAt;
                    item.Images.Add(imageDTO);
                }
            }
            return productDTOs;
        }
    }
}
