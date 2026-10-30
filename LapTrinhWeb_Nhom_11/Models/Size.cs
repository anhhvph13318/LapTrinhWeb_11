using System;
using System.Collections.Generic;

namespace LapTrinhWeb_Nhom_11.Models;

public partial class Size
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public bool IsActive { get; set; }

    public virtual ICollection<ProductDetail> ProductDetails { get; set; } = new List<ProductDetail>();
}
