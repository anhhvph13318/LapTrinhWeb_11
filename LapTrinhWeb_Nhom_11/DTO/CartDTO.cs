namespace LapTrinhWeb_Nhom_11.DTO
{
    public class CartDTO
    {
        public List<CartItemDTO> Items { get; set; } = new List<CartItemDTO>();

        public int TotalQuantity { get; set; }

        public decimal TotalAmount { get; set; }
    }
}
