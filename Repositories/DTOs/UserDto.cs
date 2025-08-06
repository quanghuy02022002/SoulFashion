namespace Repositories.DTOs
{
    public class UserDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string AvatarUrl { get; set; }
        public string Role { get; set; }

        // Thông tin xác minh (có thể null)
        public string? CCCD { get; set; }
        public string? Address { get; set; }
        public bool? Verified { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
