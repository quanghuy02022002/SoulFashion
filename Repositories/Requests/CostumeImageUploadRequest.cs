using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
namespace Repositories.Requests
{
    

    public class CostumeImageUploadRequest
    {
        [Required]
        public int CostumeId { get; set; }

        [Required]
        public IFormFile File { get; set; }
        public string? FileName { get; set; }
        public bool IsMain { get; set; } = false;

    }
}
