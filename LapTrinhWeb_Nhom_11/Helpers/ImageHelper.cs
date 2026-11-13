using LapTrinhWeb_Nhom_11.Models;

namespace LapTrinhWeb_Nhom_11.Helpers
{
    public class ImageHelper
    {
        public static Dictionary<int, string> GetMainImageUrls(AppDbContext appDbContext, List<int> productIds)
        {
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