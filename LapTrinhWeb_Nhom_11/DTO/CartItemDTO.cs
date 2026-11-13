namespace LapTrinhWeb_Nhom_11.DTO
{
    // Một dòng trong giỏ hàng
    public class CartItemDTO
    {
        public int Id { get; set; }

        public int ProductDetailId { get; set; }

        public int ProductId { get; set; }

        public string? ProductCode { get; set; }

        public string? ProductName { get; set; }

        public string? ImageUrl { get; set; }

        public string? SizeName { get; set; }

        public string? ColorName { get; set; }

        public decimal Price { get; set; }

        public int Quantity { get; set; }

        // số lượng còn trong kho
        public int Stock { get; set; }

        // false nếu sản phẩm đã ngừng bán / bị xóa hoặc kho không đủ số lượng đang có trong giỏ
        public bool IsAvailable { get; set; }
    }
}
