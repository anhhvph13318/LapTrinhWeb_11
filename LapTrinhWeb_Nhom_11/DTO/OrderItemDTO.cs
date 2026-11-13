namespace LapTrinhWeb_Nhom_11.DTO
{
    public class OrderItemDTO
    {
        public int Id { get; set; }

        public int ProductDetailId { get; set; }

        public int ProductId { get; set; }

        public string? ProductCode { get; set; }

        public string? ProductName { get; set; }

        public string? ImageUrl { get; set; }

        public string? SizeName { get; set; }

        public string? ColorName { get; set; }

        public int Quantity { get; set; }

        public decimal Price { get; set; }
    }
}
