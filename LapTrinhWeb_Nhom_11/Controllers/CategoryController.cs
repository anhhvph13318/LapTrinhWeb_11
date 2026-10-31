using LapTrinhWeb_Nhom_11.DTO;
using LapTrinhWeb_Nhom_11.Models;
using Microsoft.AspNetCore.Mvc;

namespace LapTrinhWeb_Nhom_11.Controllers
{
    [Route("/Categories")]
    public class CategoryController : Controller
    {
        AppDbContext dbcontext = new AppDbContext();
        public IActionResult Index()
        {
            return View();
        }
        [HttpGet]
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
    }
}
