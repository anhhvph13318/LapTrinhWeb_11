namespace LapTrinhWeb_Nhom_11.DTO
{
    public class ProductDTO
    {
        public int Id { get; set; }

        public string Code { get; set; } = null!;

        public string Name { get; set; } = null!;

        public decimal Price { get; set; }

        public string? Description { get; set; }

        public string? ImageUrl { get; set; }

        public int CategoryId { get; set; }

        public int Status { get; set; }

        public bool IsDeleted { get; set; }

        public DateTime CreatedAt { get; set; }

        public List<ProductDetailDTO> ProductDetails { get; set; }
        public List<ImageDTO> Images { get; set; }

    }
}
