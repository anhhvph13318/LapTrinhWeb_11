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
                    ProductDetailDTO productDetailDTO = new ProductDetailDTO();

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
                    ImageDTO imageDTO = new ImageDTO();

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

        // Thêm sản phẩm
        [HttpPost("add")]
        public IActionResult Add([FromBody] ProductDTO dto)
        {
            AppDbContext appDbContext = new AppDbContext();
            // mã sản phẩm không được trùng
            var check = appDbContext.Products.Where(c => c.Code == dto.Code).FirstOrDefault();
            if (check != null)
            {
                return BadRequest("Mã sản phẩm đã tồn tại");
            }
            Product product = new Product();
            product.Code = dto.Code;
            product.Name = dto.Name;
            product.Price = dto.Price;
            product.Description = dto.Description;
            product.ImageUrl = dto.ImageUrl;
            product.CategoryId = dto.CategoryId;
            product.Status = dto.Status;
            product.IsDeleted = false;
            product.CreatedAt = DateTime.Now;
            appDbContext.Products.Add(product);
            appDbContext.SaveChanges();
            return Ok(product.Id);
        }

        // Sửa sản phẩm
        [HttpPut("update/{id}")]
        public IActionResult Update(int id, [FromBody] ProductDTO dto)
        {
            AppDbContext appDbContext = new AppDbContext();
            var product = appDbContext.Products.Where(c => c.Id == id).FirstOrDefault();
            if (product == null)
            {
                return NotFound("Không tìm thấy sản phẩm");
            }
            // sửa mã thì cũng không được trùng với sản phẩm khác
            var check = appDbContext.Products.Where(c => c.Code == dto.Code && c.Id != id).FirstOrDefault();
            if (check != null)
            {
                return BadRequest("Mã sản phẩm đã tồn tại");
            }
            product.Code = dto.Code;
            product.Name = dto.Name;
            product.Price = dto.Price;
            product.Description = dto.Description;
            product.ImageUrl = dto.ImageUrl;
            product.CategoryId = dto.CategoryId;
            product.Status = dto.Status;
            appDbContext.SaveChanges();
            return Ok("Sửa thành công");
        }

        // Xóa mềm: chỉ đổi IsDeleted = true, không xóa trong database
        [HttpDelete("delete/{id}")]
        public IActionResult Delete(int id)
        {
            AppDbContext appDbContext = new AppDbContext();
            var product = appDbContext.Products.Where(c => c.Id == id).FirstOrDefault();
            if (product == null)
            {
                return NotFound("Không tìm thấy sản phẩm");
            }
            product.IsDeleted = true;
            appDbContext.SaveChanges();
            return Ok("Xóa thành công");
        }
    }
}