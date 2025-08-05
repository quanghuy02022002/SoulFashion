using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Repositories.Models
{
    public class CartItem
    {
        public int CartItemId { get; set; }

        public int UserId { get; set; } // Người dùng
        public int CostumeId { get; set; } // Sản phẩm

        public int Quantity { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation
        public virtual User User { get; set; }
        public virtual Costume Costume { get; set; }
    }
}