using System;
using System.Collections.Generic;

namespace LapTrinhWeb_Nhom_11.Models;

public partial class CartItem
{
    public int Id { get; set; }

    public int CartId { get; set; }

    public int ProductDetailId { get; set; }

    public int Quantity { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Cart Cart { get; set; } = null!;

    public virtual ProductDetail ProductDetail { get; set; } = null!;
}
