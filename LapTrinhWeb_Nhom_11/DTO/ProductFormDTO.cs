namespace LapTrinhWeb_Nhom_11.DTO
{
    public class ProductFormDTO
    {
        public string Code { get; set; } = null!;

        public string Name { get; set; } = null!;

        public decimal Price { get; set; }

        public string? Description { get; set; }

        public int CategoryId { get; set; }

        public int Status { get; set; }

        public List<ProductDetailDTO> Variants { get; set; } = new List<ProductDetailDTO>();

        public List<int> KeepImageIds { get; set; } = new List<int>();

        public string? MainImage { get; set; }

        public List<IFormFile> NewImages { get; set; } = new List<IFormFile>();
    }
}