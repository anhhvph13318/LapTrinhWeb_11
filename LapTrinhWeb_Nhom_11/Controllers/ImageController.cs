using LapTrinhWeb_Nhom_11.Models;
using Microsoft.AspNetCore.Mvc;

namespace LapTrinhWeb_Nhom_11.Controllers
{
    [Route("/Images")]
    public class ImageController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        [HttpGet("{id}")]
        public IActionResult GetImage(int id)
        {
            AppDbContext dbContext = new AppDbContext();
            var images = dbContext.Images.Where(c => c.Id == id).ToList();
            var image = images.Where(c => c.Id == id).FirstOrDefault();
            return File(image.Data, image.ContentType);
        }
    }
}
