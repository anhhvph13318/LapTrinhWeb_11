using LapTrinhWeb_Nhom_11.Models;

namespace LapTrinhWeb_Nhom_11.Helpers
{
    public class ImageHelper
    {
        // Lấy đường dẫn ảnh đại diện của các sản phẩm từ bảng Image (ưu tiên ảnh chính, không có thì lấy ảnh đầu tiên)
        // Key là ProductId, value có dạng /Images/{id}. Sản phẩm chưa có ảnh nào thì không có trong kết quả
        // Không dùng cột ImageUrl của bảng Product vì cột này có thể bị trống hoặc cũ với dữ liệu nhập tay
        public static Dictionary<int, string> GetMainImageUrls(AppDbContext appDbContext, List<int> productIds)
        {
            // không lấy cột Data (nội dung ảnh) cho nhẹ
            var images = appDbContext.Images
                .Where(c => productIds.Contains(c.ProductId))
                .Select(c => new { c.Id, c.ProductId, c.IsMain, c.SortOrder })
                .ToList();
            Dictionary<int, string> result = new Dictionary<int, string>();
            foreach (var group in images.GroupBy(c => c.ProductId))
            {
                var image = group.OrderByDescending(c => c.IsMain).ThenBy(c => c.SortOrder).First();
                result[group.Key] = "/Images/" + image.Id;
            }
            return result;
        }
    }
}