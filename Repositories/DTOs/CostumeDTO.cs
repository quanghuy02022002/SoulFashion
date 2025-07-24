using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Repositories.DTOs
{

    public class CostumeDTO
    {
        public int CostumeId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        public string? Description { get; set; }

        [Range(1, int.MaxValue)]
        public int CategoryId { get; set; }

        [Range(1, int.MaxValue)]
        public int OwnerId { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? PriceSale { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? PriceRent { get; set; }

        [Range(0, int.MaxValue)]
        public int Quantity { get; set; }

        [StringLength(20)]
        public string? Size { get; set; }

        [StringLength(50)]
        public string? Condition { get; set; }

        [Required]
        [RegularExpression("male|female|unisex", ErrorMessage = "Gender must be male, female, or unisex")]
        public string Gender { get; set; }

        public bool IsActive { get; set; } = true;
        public List<CostumeImageDTO> Images { get; set; } = new();

    }

}
