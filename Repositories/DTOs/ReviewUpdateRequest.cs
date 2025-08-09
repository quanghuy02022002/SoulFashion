// Repositories/Requests/ReviewUpdateRequest.cs
using System.ComponentModel.DataAnnotations;

namespace Repositories.Requests
{
    public class ReviewUpdateRequest
    {
        [Required, Range(1, 5)]
        public int Rating { get; set; }

        [StringLength(2000)]
        public string Comment { get; set; }
    }
}
    