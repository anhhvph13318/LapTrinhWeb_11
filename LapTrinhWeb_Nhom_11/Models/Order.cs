using System;
using System.Collections.Generic;

namespace LapTrinhWeb_Nhom_11.Models;

public partial class Order
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

    public virtual Account? Account { get; set; }

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
}
