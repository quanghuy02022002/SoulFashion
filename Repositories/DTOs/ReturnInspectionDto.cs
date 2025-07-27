// Repositories/DTOs/ReturnInspectionDto.cs
namespace Repositories.DTOs
{
    public class ReturnInspectionDto
    {
        public int OrderId { get; set; }
        public string Condition { get; set; } = string.Empty;
        public decimal PenaltyAmount { get; set; }
        public string? Note { get; set; }
    }
}
