namespace LapTrinhWeb_Nhom_11.DTO
{
    // Dữ liệu form thêm / sửa sản phẩm (gửi dạng multipart/form-data vì có kèm ảnh)
    public class ProductFormDTO
    {
        public string Code { get; set; } = null!;

        public string Name { get; set; } = null!;

        public decimal Price { get; set; }

        public string? Description { get; set; }

        public int CategoryId { get; set; }

        public int Status { get; set; }

        // các biến thể: màu, size, số lượng (Id = 0 là biến thể mới)
        public List<ProductDetailDTO> Variants { get; set; } = new List<ProductDetailDTO>();

        // id các ảnh cũ muốn giữ lại (ảnh cũ không có trong danh sách này sẽ bị xóa)
        public List<int> KeepImageIds { get; set; } = new List<int>();

        // ảnh chính: "old:5" là ảnh cũ có id 5, "new:0" là ảnh mới thứ 0 trong NewImages
        public string? MainImage { get; set; }

        // các ảnh mới upload
        public List<IFormFile> NewImages { get; set; } = new List<IFormFile>();
    }
}