using System.ComponentModel.DataAnnotations;

namespace Repositories.DTOs;

public class CollaboratorEarningCreateDto
{
    [Required] public int UserId { get; set; }
    [Required] public int OrderItemId { get; set; }
    [Required, Range(0, double.MaxValue)] public decimal EarningAmount { get; set; }
    [MaxLength(50)] public string Status { get; set; } = "pending"; // pending|paid|cancelled
}

public class CollaboratorEarningUpdateDto
{
    [Required] public int UserId { get; set; }
    [Required] public int OrderItemId { get; set; }
    [Required, Range(0, double.MaxValue)] public decimal EarningAmount { get; set; }
    [MaxLength(50)] public string Status { get; set; } = "pending";
}

public class CollaboratorEarningStatusPatchDto
{
    [Required, MaxLength(50)] public string Status { get; set; } = "pending";
}

public class CollaboratorEarningResponseDto
{
    public int EarningId { get; set; }
    public int UserId { get; set; }
    public int OrderItemId { get; set; }
    public decimal EarningAmount { get; set; }
    public string Status { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class PagedResult<T>
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public long TotalItems { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
    public List<T> Items { get; set; } = new();
}