namespace LapTrinhWeb_Nhom_11.DTO
{
    // Dữ liệu khi bấm đặt hàng
    public class CheckoutDTO
    {
        public string? CustomerName { get; set; }

        public string? Phone { get; set; }

        public string? Address { get; set; }

        public string? Note { get; set; }

        // 0 = COD (thanh toán khi nhận hàng), 1 = VNPay
        public int PaymentMethod { get; set; }

        // id các dòng trong giỏ muốn mua; để trống thì mua cả giỏ
        public List<int> CartItemIds { get; set; } = new List<int>();
    }
}