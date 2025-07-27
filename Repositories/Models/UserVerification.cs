using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.Models
{
    public class UserVerification
    {
        [Key]
        public int VerificationId { get; set; }
        public int UserId { get; set; }
        public string CCCD { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public bool Verified { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public User? User { get; set; }
    }

}
