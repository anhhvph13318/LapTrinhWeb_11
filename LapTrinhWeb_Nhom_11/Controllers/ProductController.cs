using LapTrinhWeb_Nhom_11.DTO;
using LapTrinhWeb_Nhom_11.Helpers;
using LapTrinhWeb_Nhom_11.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LapTrinhWeb_Nhom_11.Controllers
{
    [Route("/products")]
    public class ProductController : Controller
    {
        [HttpGet("")]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("GetAll")]
        public List<ProductDTO> GetAll()
        {
            AppDbContext appDbContext = new AppDbContext();
            var product = appDbContext.Products.ToList();
            return ToDTOs(appDbContext, product);
        }

        // Danh sách sản phẩm cho trang admin, có bộ lọc
        [AdminOnly]
        [HttpGet("admin")]
        public List<ProductDTO> GetAdmin(string? keyword, int? categoryId, int? status, bool? deleted, decimal? minPrice, decimal? maxPrice)
        {
            AppDbContext appDbContext = new AppDbContext();
            var query = appDbContext.Products.AsQueryable();
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();
                query = query.Where(c => c.Name.Contains(keyword) || c.Code.Contains(keyword));
            }
            if (categoryId != null)
            {
                query = query.Where(c => c.CategoryId == categoryId);
            }
            if (status != null)
            {
                query = query.Where(c => c.Status == status);
            }
            if (deleted != null)
            {
                query = query.Where(c => c.IsDeleted == deleted);
            }
            if (minPrice != null)
            {
                query = query.Where(c => c.Price >= minPrice);
            }
            if (maxPrice != null)
            {
                query = query.Where(c => c.Price <= maxPrice);
            }
            var product = query.OrderByDescending(c => c.Id).ToList();
            return ToDTOs(appDbContext, product);
        }

        // Danh sách danh mục, màu, size để đổ vào các ô chọn trong form
        [AdminOnly]
        [HttpGet("options")]
        public IActionResult Options()
        {
            AppDbContext appDbContext = new AppDbContext();
            var categories = appDbContext.Categories.Select(c => new { c.Id, c.Name, c.IsDeleted }).ToList();
            var colors = appDbContext.Colors.Select(c => new { c.Id, c.Name, c.HexCode, c.IsActive }).ToList();
            var sizes = appDbContext.Sizes.Select(c => new { c.Id, c.Name, c.IsActive }).ToList();
            return Ok(new { categories = categories, colors = colors, sizes = sizes });
        }

        // Thêm sản phẩm kèm biến thể và ảnh trong 1 lần
        [AdminOnly]
        [RequestSizeLimit(50000000)]
        [RequestFormLimits(MultipartBodyLengthLimit = 50000000)]
        [HttpPost("add")]
        public IActionResult Add([FromForm] ProductFormDTO form)
        {
            AppDbContext appDbContext = new AppDbContext();
            string? error = CheckForm(appDbContext, form, 0);
            if (error != null)
            {
                return BadRequest(error);
            }
            try
            {
                Product product = new Product();
                product.Code = form.Code.Trim();
                product.Name = form.Name.Trim();
                product.Price = form.Price;
                product.Description = form.Description;
                product.CategoryId = form.CategoryId;
                product.Status = form.Status;
                product.IsDeleted = false;
                product.CreatedAt = DateTime.Now;
                foreach (var v in form.Variants)
                {
                    ProductDetail detail = new ProductDetail();
                    detail.ColorId = v.ColorId;
                    detail.SizeId = v.SizeId;
                    detail.Quantity = v.Quantity;
                    product.ProductDetails.Add(detail);
                }
                appDbContext.Products.Add(product);
                appDbContext.SaveChanges();

                // lưu ảnh (xong thì gán ảnh chính vào product.ImageUrl)
                SaveImages(product, form);
                appDbContext.SaveChanges();
                return Ok(new { message = "Thêm thành công", id = product.Id });
            }
            catch (Exception ex)
            {
                return BadRequest("Lưu thất bại: " + (ex.InnerException?.Message ?? ex.Message));
            }
        }

        // Sửa sản phẩm kèm biến thể và ảnh trong 1 lần
        [AdminOnly]
        [RequestSizeLimit(50000000)]
        [RequestFormLimits(MultipartBodyLengthLimit = 50000000)]
        [HttpPut("update/{id}")]
        public IActionResult Update(int id, [FromForm] ProductFormDTO form)
        {
            AppDbContext appDbContext = new AppDbContext();
            var product = appDbContext.Products.Where(c => c.Id == id).FirstOrDefault();
            if (product == null)
            {
                return NotFound("Không tìm thấy sản phẩm");
            }
            string? error = CheckForm(appDbContext, form, id);
            if (error != null)
            {
                return BadRequest(error);
            }
            try
            {
                product.Code = form.Code.Trim();
                product.Name = form.Name.Trim();
                product.Price = form.Price;
                product.Description = form.Description;
                product.CategoryId = form.CategoryId;
                product.Status = form.Status;
                string note = SaveVariants(appDbContext, product, form.Variants);
                appDbContext.SaveChanges();

                SaveImages(product, form);
                appDbContext.SaveChanges();
                return Ok(new { message = "Sửa thành công" + note });
            }
            catch (Exception ex)
            {
                return BadRequest("Lưu thất bại: " + (ex.InnerException?.Message ?? ex.Message));
            }
        }

        // Xóa mềm: chỉ đổi IsDeleted = true, không xóa trong database
        [AdminOnly]
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

        // Khôi phục sản phẩm đã xóa mềm
        [AdminOnly]
        [HttpPut("restore/{id}")]
        public IActionResult Restore(int id)
        {
            AppDbContext appDbContext = new AppDbContext();
            var product = appDbContext.Products.Where(c => c.Id == id).FirstOrDefault();
            if (product == null)
            {
                return NotFound("Không tìm thấy sản phẩm");
            }
            product.IsDeleted = false;
            appDbContext.SaveChanges();
            return Ok("Khôi phục thành công");
        }

        // ================== các hàm dùng chung ==================

        // Đổi danh sách Product sang ProductDTO (kèm biến thể và thông tin ảnh)
        private List<ProductDTO> ToDTOs(AppDbContext appDbContext, List<Product> products)
        {
            List<ProductDTO> productDTOs = new List<ProductDTO>();
            foreach (var p in products)
            {
                ProductDTO productDTO = new ProductDTO();
                productDTO.Id = p.Id;
                productDTO.Name = p.Name;
                productDTO.Price = p.Price;
                productDTO.Code = p.Code;
                productDTO.CategoryId = p.CategoryId;
                productDTO.CreatedAt = p.CreatedAt;
                productDTO.Description = p.Description;
                productDTO.ImageUrl = p.ImageUrl;
                productDTO.IsDeleted = p.IsDeleted;
                productDTO.Status = p.Status;
                productDTO.ProductDetails = new List<ProductDetailDTO>();
                productDTO.Images = new List<ImageDTO>();

                var productdetails = appDbContext.ProductDetails.Where(c => c.ProductId == p.Id).ToList();
                foreach (var item1 in productdetails)
                {
                    ProductDetailDTO productDetailDTO = new ProductDetailDTO();
                    productDetailDTO.Id = item1.Id;
                    productDetailDTO.ColorId = item1.ColorId;
                    productDetailDTO.SizeId = item1.SizeId;
                    productDetailDTO.Quantity = item1.Quantity;
                    productDetailDTO.ProductId = item1.ProductId;
                    productDTO.ProductDetails.Add(productDetailDTO);
                }

                // chỉ lấy thông tin ảnh, không lấy cột Data cho nhẹ
                var images = appDbContext.Images.Where(c => c.ProductId == p.Id)
                    .Select(c => new { c.Id, c.ProductId, c.FileName, c.ContentType, c.IsMain, c.SortOrder, c.Version, c.CreatedAt })
                    .OrderBy(c => c.SortOrder)
                    .ToList();
                foreach (var item1 in images)
                {
                    ImageDTO imageDTO = new ImageDTO();
                    imageDTO.Id = item1.Id;
                    imageDTO.ProductId = item1.ProductId;
                    imageDTO.FileName = item1.FileName;
                    imageDTO.ContentType = item1.ContentType;
                    imageDTO.ImageUrl = "/Images/" + item1.Id;
                    imageDTO.IsMain = item1.IsMain;
                    imageDTO.SortOrder = item1.SortOrder;
                    imageDTO.Version = item1.Version;
                    imageDTO.CreatedAt = item1.CreatedAt;
                    productDTO.Images.Add(imageDTO);
                }
                productDTOs.Add(productDTO);
            }
            return productDTOs;
        }

        // Kiểm tra dữ liệu form, có lỗi thì trả về câu báo lỗi, không lỗi trả về null
        // id = 0 là thêm mới, id > 0 là sửa (để kiểm tra trùng mã không tính chính nó)
        private string? CheckForm(AppDbContext appDbContext, ProductFormDTO form, int id)
        {
            if (string.IsNullOrWhiteSpace(form.Code))
            {
                return "Mã sản phẩm không được để trống";
            }
            string code = form.Code.Trim();
            if (code.Length > 20)
            {
                return "Mã sản phẩm tối đa 20 ký tự";
            }
            if (string.IsNullOrWhiteSpace(form.Name))
            {
                return "Tên sản phẩm không được để trống";
            }
            if (form.Name.Trim().Length > 200)
            {
                return "Tên sản phẩm tối đa 200 ký tự";
            }
            if (form.Price < 0)
            {
                return "Giá không được âm";
            }
            var category = appDbContext.Categories.Where(c => c.Id == form.CategoryId && c.IsDeleted == false).FirstOrDefault();
            if (category == null)
            {
                return "Danh mục không tồn tại hoặc đã bị xóa";
            }
            var sameCode = appDbContext.Products.Where(c => c.Code == code && c.Id != id).FirstOrDefault();
            if (sameCode != null)
            {
                return "Mã sản phẩm đã tồn tại";
            }

            // biến thể
            if (form.Variants.Count == 0)
            {
                return "Sản phẩm phải có ít nhất 1 biến thể (màu, size, số lượng)";
            }
            foreach (var v in form.Variants)
            {
                if (v.Quantity < 0)
                {
                    return "Số lượng không được âm";
                }
                bool colorOk = appDbContext.Colors.Any(c => c.Id == v.ColorId);
                bool sizeOk = appDbContext.Sizes.Any(c => c.Id == v.SizeId);
                if (colorOk == false || sizeOk == false)
                {
                    return "Màu hoặc size không tồn tại";
                }
            }
            bool duplicate = form.Variants.GroupBy(v => new { v.ColorId, v.SizeId }).Any(g => g.Count() > 1);
            if (duplicate)
            {
                return "Có biến thể bị trùng cả màu và size";
            }

            // ảnh mới
            string[] allowed = { "image/jpeg", "image/png", "image/gif", "image/webp" };
            foreach (var file in form.NewImages)
            {
                if (file.Length == 0)
                {
                    return "Có file ảnh bị rỗng";
                }
                if (file.Length > 5 * 1024 * 1024)
                {
                    return "Mỗi ảnh tối đa 5MB";
                }
                if (allowed.Contains(file.ContentType.ToLower()) == false)
                {
                    return "Chỉ nhận ảnh jpg, png, gif, webp";
                }
            }
            return null;
        }

        // Cập nhật biến thể khi sửa: Id > 0 là sửa, Id = 0 là thêm mới, biến thể không còn trong form là bị xóa
        // Biến thể đã có trong đơn hàng thì không xóa được (sẽ lỗi khóa ngoại) nên chỉ đặt số lượng về 0
        private string SaveVariants(AppDbContext appDbContext, Product product, List<ProductDetailDTO> variants)
        {
            var oldDetails = appDbContext.ProductDetails.Where(c => c.ProductId == product.Id).ToList();
            List<string> keepNames = new List<string>();
            foreach (var old in oldDetails)
            {
                bool stillHas = variants.Any(v => v.Id == old.Id);
                if (stillHas)
                {
                    continue;
                }
                bool inOrder = appDbContext.OrderDetails.Any(c => c.ProductDetailId == old.Id);
                if (inOrder)
                {
                    old.Quantity = 0;
                    var color = appDbContext.Colors.Where(c => c.Id == old.ColorId).FirstOrDefault();
                    var size = appDbContext.Sizes.Where(c => c.Id == old.SizeId).FirstOrDefault();
                    keepNames.Add(size?.Name + "/" + color?.Name);
                }
                else
                {
                    var cartItems = appDbContext.CartItems.Where(c => c.ProductDetailId == old.Id).ToList();
                    appDbContext.CartItems.RemoveRange(cartItems);
                    appDbContext.ProductDetails.Remove(old);
                }
            }
            foreach (var v in variants)
            {
                if (v.Id > 0)
                {
                    var detail = oldDetails.Where(c => c.Id == v.Id).FirstOrDefault();
                    if (detail != null)
                    {
                        detail.ColorId = v.ColorId;
                        detail.SizeId = v.SizeId;
                        detail.Quantity = v.Quantity;
                    }
                }
                else
                {
                    ProductDetail detail = new ProductDetail();
                    detail.ProductId = product.Id;
                    detail.ColorId = v.ColorId;
                    detail.SizeId = v.SizeId;
                    detail.Quantity = v.Quantity;
                    appDbContext.ProductDetails.Add(detail);
                }
            }
            if (keepNames.Count > 0)
            {
                return ". Lưu ý: các biến thể (size/màu) " + string.Join(", ", keepNames) + " đã có trong đơn hàng nên không xóa được, chỉ đặt số lượng về 0";
            }
            return "";
        }

        // Lưu ảnh: xóa ảnh cũ không giữ, thêm ảnh mới, chọn ảnh chính rồi gán vào product.ImageUrl
        // Mỗi thao tác dùng AppDbContext riêng vì trong AppDbContext, Product và Image đang cấu hình quan hệ 1-1,
        // nếu thêm nhiều ảnh trong cùng 1 context thì EF có thể xóa nhầm ảnh trước đó
        private void SaveImages(Product product, ProductFormDTO form)
        {
            AppDbContext db = new AppDbContext();
            // xóa các ảnh không còn giữ lại (khi thêm mới thì chưa có ảnh nào)
            db.Images.Where(c => c.ProductId == product.Id && !form.KeepImageIds.Contains(c.Id)).ExecuteDelete();
            // bỏ trạng thái ảnh chính của các ảnh còn lại, chọn lại ở dưới
            db.Images.Where(c => c.ProductId == product.Id).ExecuteUpdate(s => s.SetProperty(c => c.IsMain, false));

            int sort = db.Images.Where(c => c.ProductId == product.Id).Max(c => (int?)c.SortOrder) ?? 0;
            List<int> newImageIds = new List<int>();
            foreach (var file in form.NewImages)
            {
                MemoryStream ms = new MemoryStream();
                file.CopyTo(ms);
                sort++;
                Image image = new Image();
                image.ProductId = product.Id;
                image.FileName = Path.GetFileName(file.FileName);
                image.ContentType = file.ContentType;
                image.Data = ms.ToArray();
                image.IsMain = false;
                image.SortOrder = sort;
                image.Version = 1;
                image.CreatedAt = DateTime.Now;
                AppDbContext imageDb = new AppDbContext();
                imageDb.Images.Add(image);
                imageDb.SaveChanges();
                newImageIds.Add(image.Id);
            }

            // chọn ảnh chính
            int mainId = 0;
            string main = form.MainImage ?? "";
            if (main.StartsWith("old:"))
            {
                int oldId;
                if (int.TryParse(main.Substring(4), out oldId))
                {
                    // chỉ nhận nếu ảnh này thuộc sản phẩm
                    mainId = db.Images.Where(c => c.Id == oldId && c.ProductId == product.Id).Select(c => c.Id).FirstOrDefault();
                }
            }
            else if (main.StartsWith("new:"))
            {
                int index;
                if (int.TryParse(main.Substring(4), out index) && index >= 0 && index < newImageIds.Count)
                {
                    mainId = newImageIds[index];
                }
            }
            if (mainId == 0)
            {
                // không chọn thì lấy ảnh đầu tiên
                mainId = db.Images.Where(c => c.ProductId == product.Id).OrderBy(c => c.SortOrder).Select(c => c.Id).FirstOrDefault();
            }
            if (mainId != 0)
            {
                db.Images.Where(c => c.Id == mainId).ExecuteUpdate(s => s.SetProperty(c => c.IsMain, true));
                product.ImageUrl = "/Images/" + mainId;
            }
            else
            {
                product.ImageUrl = null;
            }
        }
        // Hiển thị trang chi tiết sản phẩm
        [HttpGet("detail/{id}")]
        public IActionResult Detail(int id)
        {
            using (AppDbContext appDbContext = new AppDbContext())
            {
                var product = appDbContext.Products
                    .FirstOrDefault(p => p.Id == id && !p.IsDeleted);

                if (product == null)
                {
                    return NotFound("Không tìm thấy sản phẩm");
                }

                ViewBag.ProductId = id;

                return View("~/Views/Product/Detail.cshtml");
            }
        }
    }
}