using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.DTOs
{
    public class UpdateAccountDto
    {
        [StringLength(100)]
        public string FullName { get; set; }

        [Phone]
        public string Phone { get; set; }

        [StringLength(12, MinimumLength = 12)]
        public string? CCCD { get; set; }
        [StringLength(200)]
        public string? Address { get; set; }
        public IFormFile? VerificationImage { get; set; } // ảnh CCCD
    }


}
