using LapTrinhWeb_Nhom_11.DTO;
using LapTrinhWeb_Nhom_11.Models;
using Microsoft.AspNetCore.Mvc;

namespace LapTrinhWeb_Nhom_11.Controllers
{
    [Route("/Categories")]
    public class CategoryController : Controller
    {
        AppDbContext dbcontext = new AppDbContext();
        [HttpGet("")]
        public IActionResult Index()
        {
            return View();
        }
        [HttpGet("GetAll")]
        public List<CategoryDTO> GetAll()
        {
            var categories = dbcontext.Categories.ToList();
            CategoryDTO categoryDTO = new CategoryDTO();
            List<CategoryDTO> categoryDTOs = categories.Select(p => new CategoryDTO
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                IsDeleted = p.IsDeleted
            }).ToList();
            return categoryDTOs;
        }
        [HttpPost("add")]
        public IActionResult Add([FromBody] CategoryDTO dto)
        {
            AppDbContext appDbContext = new AppDbContext();
            Category category = new Category();
            category.Name = dto.Name;
            category.Description = dto.Description;
            category.IsDeleted = false;
            appDbContext.Categories.Add(category);
            appDbContext.SaveChanges();
            return Ok(category.Id);
        }
        [HttpPut("update/{id}")]
        public IActionResult Update(int id, [FromBody] CategoryDTO dto)
        {
            AppDbContext appDbContext = new AppDbContext();
            var category = appDbContext.Categories.Where(c => c.Id == id).FirstOrDefault();
            if (category == null)
            {
                return NotFound("Không tìm thấy danh mục");
            }
            category.Name = dto.Name;
            category.Description = dto.Description;
            appDbContext.SaveChanges();
            return Ok("Sửa thành công");
        }
        [HttpDelete("delete/{id}")]
        public IActionResult Delete(int id)
        {
            AppDbContext appDbContext = new AppDbContext();
            var category = appDbContext.Categories.Where(c => c.Id == id).FirstOrDefault();
            if (category == null)
            {
                return NotFound("Không tìm thấy danh mục");
            }
            category.IsDeleted = true;
            appDbContext.SaveChanges();
            return Ok("Xóa thành công");
        }
        [HttpPut("restore/{id}")]
        public IActionResult Restore(int id)
        {
            AppDbContext appDbContext = new AppDbContext();
            var category = appDbContext.Categories.Where(c => c.Id == id).FirstOrDefault();
            if (category == null)
            {
                return NotFound("Không tìm thấy danh mục");
            }
            category.IsDeleted = false;
            appDbContext.SaveChanges();
            return Ok("Khôi phục thành công");
        }
    }
}
