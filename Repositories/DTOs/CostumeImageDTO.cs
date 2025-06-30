using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.DTOs
{
    public class CostumeImageDTO
    {
        public int ImageId { get; set; }

        [Range(1, int.MaxValue)]
        public int CostumeId { get; set; }

        [Required]
        [Url]
        public string ImageUrl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }

}
