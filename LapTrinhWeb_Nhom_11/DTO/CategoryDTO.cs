namespace LapTrinhWeb_Nhom_11.DTO
{
    public class CategoryDTO
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        public bool IsDeleted { get; set; }
    }
}
