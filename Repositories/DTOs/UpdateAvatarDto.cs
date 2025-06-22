using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repositories.DTOs
{
    public class UpdateAvatarDto
    {
        [Required]
        public IFormFile Avatar { get; set; }
    }

}
