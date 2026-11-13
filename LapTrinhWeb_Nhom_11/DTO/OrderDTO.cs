namespace LapTrinhWeb_Nhom_11.DTO
{
    public class OrderDTO
    {
        public int Id { get; set; }

        public string OrderCode { get; set; } = null!;

        public string CustomerName { get; set; } = null!;

        public string Phone { get; set; } = null!;

        public string Address { get; set; } = null!;

        public string? Note { get; set; }

        public decimal TotalAmount { get; set; }

        public int PaymentMethod { get; set; }

        public int PaymentStatus { get; set; }

        public int Status { get; set; }

        public DateTime CreatedAt { get; set; }

        public int? AccountId { get; set; }

        public string? TrackingCode { get; set; }

        public int ItemCount { get; set; }

        public List<OrderItemDTO> Items { get; set; } = new List<OrderItemDTO>();
    }
}
