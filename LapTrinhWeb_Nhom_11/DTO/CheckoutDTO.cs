namespace LapTrinhWeb_Nhom_11.DTO
{
    public class CheckoutDTO
    {
        public string? CustomerName { get; set; }

        public string? Phone { get; set; }

        public string? Address { get; set; }

        public string? Note { get; set; }

        public int PaymentMethod { get; set; }

        public List<int> CartItemIds { get; set; } = new List<int>();
    }
}