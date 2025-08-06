using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.DTOs
{
    public class SubmitVerificationDto
    {
        public string? Address { get; set; }
        [StringLength(12), MinLength(12), MaxLength(12)]
        public string CCCD { get; set; } = null!;
        public IFormFile VerificationImage { get; set; } = null!;
    }

}
