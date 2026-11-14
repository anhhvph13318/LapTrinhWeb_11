using LapTrinhWeb_Nhom_11.DTO;
using LapTrinhWeb_Nhom_11.Helpers;
using LapTrinhWeb_Nhom_11.Models;
using Microsoft.AspNetCore.Mvc;

namespace LapTrinhWeb_Nhom_11.Controllers
{
    [Route("/sizes")]
    public class SizeController : Controller
    {
        [AdminOnly]
        [HttpPost("add")]
        public IActionResult Add([FromBody] SizeFormDTO dto)
        {
            string? error = Check(dto, out string name);
            if (error != null)
            {
                return BadRequest(error);
            }

            AppDbContext appDbContext = new AppDbContext();
            var size = appDbContext.Sizes.Where(c => c.Name == name).FirstOrDefault();
            string message = "Thêm size thành công";
            if (size != null)
            {
                if (size.IsActive)
                {
                    return BadRequest("Size này đã có trong danh sách");
                }
                size.IsActive = true;
                message = "Size này đã có từ trước nhưng đang bị ẩn, đã bật lại";
            }
            else
            {
                size = new Size();
                size.Name = name;
                size.IsActive = true;
                appDbContext.Sizes.Add(size);
            }
            appDbContext.SaveChanges();
            return Ok(ToResult(appDbContext, size, message));
        }

        [AdminOnly]
        [HttpPut("update/{id}")]
        public IActionResult Update(int id, [FromBody] SizeFormDTO dto)
        {
            AppDbContext appDbContext = new AppDbContext();
            var size = appDbContext.Sizes.Where(c => c.Id == id).FirstOrDefault();
            if (size == null)
            {
                return NotFound("Không tìm thấy size");
            }
            string? error = Check(dto, out string name);
            if (error != null)
            {
                return BadRequest(error);
            }
            var same = appDbContext.Sizes.Where(c => c.Name == name && c.Id != id).FirstOrDefault();
            if (same != null)
            {
                return BadRequest("Đã có size khác trùng tên này");
            }
            size.Name = name;
            appDbContext.SaveChanges();
            return Ok(ToResult(appDbContext, size, "Đã lưu size"));
        }

        [AdminOnly]
        [HttpPut("hide/{id}")]
        public IActionResult Hide(int id)
        {
            AppDbContext appDbContext = new AppDbContext();
            var size = appDbContext.Sizes.Where(c => c.Id == id).FirstOrDefault();
            if (size == null)
            {
                return NotFound("Không tìm thấy size");
            }
            size.IsActive = false;
            appDbContext.SaveChanges();
            return Ok(ToResult(appDbContext, size, "Đã ẩn size"));
        }

        [AdminOnly]
        [HttpPut("show/{id}")]
        public IActionResult Show(int id)
        {
            AppDbContext appDbContext = new AppDbContext();
            var size = appDbContext.Sizes.Where(c => c.Id == id).FirstOrDefault();
            if (size == null)
            {
                return NotFound("Không tìm thấy size");
            }
            size.IsActive = true;
            appDbContext.SaveChanges();
            return Ok(ToResult(appDbContext, size, "Đã hiện lại size"));
        }


        private string? Check(SizeFormDTO dto, out string name)
        {
            name = (dto.Name ?? "").Trim();
            if (name == "")
            {
                return "Tên size không được để trống";
            }
            if (name.Length > 20)
            {
                return "Tên size tối đa 20 ký tự";
            }
            return null;
        }

        private object ToResult(AppDbContext appDbContext, Size size, string message)
        {
            int usedCount = appDbContext.ProductDetails.Count(c => c.SizeId == size.Id);
            return new { id = size.Id, name = size.Name, isActive = size.IsActive, usedCount = usedCount, message = message };
        }
    }
}