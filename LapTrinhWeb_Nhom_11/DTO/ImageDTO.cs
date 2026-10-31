namespace LapTrinhWeb_Nhom_11.DTO
{
    public class ImageDTO
    {
        public int Id { get; set; }

        public int ProductId { get; set; }

        public string? FileName { get; set; }

        public string ContentType { get; set; } = null!;

        public string ImageUrl { get; set; }
        public byte[] Data { get; set; } = null!;

        public bool IsMain { get; set; }

        public int SortOrder { get; set; }

        public int Version { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
