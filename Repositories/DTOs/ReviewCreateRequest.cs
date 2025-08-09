// Repositories/Requests/ReviewCreateRequest.cs
using System.ComponentModel.DataAnnotations;

namespace Repositories.Requests
{
    public class ReviewCreateRequest
    {
        [Required, Range(1, int.MaxValue)]
        public int UserId { get; set; }

        [Required, Range(1, int.MaxValue)]
        public int CostumeId { get; set; }

        [Required, Range(1, 5)]
        public int Rating { get; set; }

        [StringLength(2000)]
        public string Comment { get; set; }
    }
}
