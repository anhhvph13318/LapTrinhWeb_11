using System;
using System.Collections.Generic;

namespace LapTrinhWeb_Nhom_11.Models;

public partial class Image
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public string? FileName { get; set; }

    public string ContentType { get; set; } = null!;

    public byte[] Data { get; set; } = null!;

    public bool IsMain { get; set; }

    public int SortOrder { get; set; }

    public int Version { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Product Product { get; set; } = null!;
}
