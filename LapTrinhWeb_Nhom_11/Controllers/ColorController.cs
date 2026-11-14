using LapTrinhWeb_Nhom_11.DTO;
using LapTrinhWeb_Nhom_11.Helpers;
using LapTrinhWeb_Nhom_11.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

namespace LapTrinhWeb_Nhom_11.Controllers
{
    [Route("/colors")]
    public class ColorController : Controller
    {
        [AdminOnly]
        [HttpPost("add")]
        public IActionResult Add([FromBody] ColorFormDTO dto)
        {
            string? error = Check(dto, out string name, out string? hexCode);
            if (error != null)
            {
                return BadRequest(error);
            }

            AppDbContext appDbContext = new AppDbContext();
            var color = appDbContext.Colors.Where(c => c.Name == name).FirstOrDefault();
            string message = "Thêm màu thành công";
            if (color != null)
            {
                if (color.IsActive)
                {
                    return BadRequest("Màu này đã có trong danh sách");
                }
                color.IsActive = true;
                if (hexCode != null)
                {
                    color.HexCode = hexCode;
                }
                message = "Màu này đã có từ trước nhưng đang bị ẩn, đã bật lại";
            }
            else
            {
                color = new Color();
                color.Name = name;
                color.HexCode = hexCode;
                color.IsActive = true;
                appDbContext.Colors.Add(color);
            }
            appDbContext.SaveChanges();
            return Ok(ToResult(appDbContext, color, message));
        }
        [AdminOnly]
        [HttpPut("update/{id}")]
        public IActionResult Update(int id, [FromBody] ColorFormDTO dto)
        {
            AppDbContext appDbContext = new AppDbContext();
            var color = appDbContext.Colors.Where(c => c.Id == id).FirstOrDefault();
            if (color == null)
            {
                return NotFound("Không tìm thấy màu");
            }
            string? error = Check(dto, out string name, out string? hexCode);
            if (error != null)
            {
                return BadRequest(error);
            }
            var same = appDbContext.Colors.Where(c => c.Name == name && c.Id != id).FirstOrDefault();
            if (same != null)
            {
                return BadRequest("Đã có màu khác trùng tên này");
            }
            color.Name = name;
            color.HexCode = hexCode;
            appDbContext.SaveChanges();
            return Ok(ToResult(appDbContext, color, "Đã lưu màu"));
        }
        [AdminOnly]
        [HttpPut("hide/{id}")]
        public IActionResult Hide(int id)
        {
            AppDbContext appDbContext = new AppDbContext();
            var color = appDbContext.Colors.Where(c => c.Id == id).FirstOrDefault();
            if (color == null)
            {
                return NotFound("Không tìm thấy màu");
            }
            color.IsActive = false;
            appDbContext.SaveChanges();
            return Ok(ToResult(appDbContext, color, "Đã ẩn màu"));
        }
        [AdminOnly]
        [HttpPut("show/{id}")]
        public IActionResult Show(int id)
        {
            AppDbContext appDbContext = new AppDbContext();
            var color = appDbContext.Colors.Where(c => c.Id == id).FirstOrDefault();
            if (color == null)
            {
                return NotFound("Không tìm thấy màu");
            }
            color.IsActive = true;
            appDbContext.SaveChanges();
            return Ok(ToResult(appDbContext, color, "Đã hiện lại màu"));
        }
        private string? Check(ColorFormDTO dto, out string name, out string? hexCode)
        {
            name = (dto.Name ?? "").Trim();
            hexCode = string.IsNullOrWhiteSpace(dto.HexCode) ? null : dto.HexCode.Trim();
            if (name == "")
            {
                return "Tên màu không được để trống";
            }
            if (name.Length > 50)
            {
                return "Tên màu tối đa 50 ký tự";
            }
            if (hexCode != null && Regex.IsMatch(hexCode, "^#[0-9a-fA-F]{6}$") == false)
            {
                return "Mã màu phải có dạng #RRGGBB, ví dụ #FF0000";
            }
            return null;
        }

        private object ToResult(AppDbContext appDbContext, Color color, string message)
        {
            int usedCount = appDbContext.ProductDetails.Count(c => c.ColorId == color.Id);
            return new { id = color.Id, name = color.Name, hexCode = color.HexCode, isActive = color.IsActive, usedCount = usedCount, message = message };
        }
    }
}