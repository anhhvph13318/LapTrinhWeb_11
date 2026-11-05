namespace LapTrinhWeb_Nhom_11.DTO
{
    public class ProductDetailDTO
    {
        public int Id { get; set; }

        public int ProductId { get; set; }

        public int Quantity { get; set; }

        public int SizeId { get; set; }

        public int ColorId { get; set; }

        public string Name { get; set; }

        public List<SizeDTO> Sizes { get; set; } = [];

        public List<ColorDTO> Colors { get; set; } = [];

    }
}
