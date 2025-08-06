using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class UserVerificationDto
{
    public int VerificationId { get; set; }
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string CCCD { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}
