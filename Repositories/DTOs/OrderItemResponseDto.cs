namespace Repositories.DTOs
{
    public class OrderItemResponseDto
    {
        public int OrderItemId { get; set; }
        public int OrderId { get; set; }
        public int CostumeId { get; set; }
        public string CostumeName { get; set; }
        public string CostumeImageUrl { get; set; } // 💡 ảnh đại diện
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
