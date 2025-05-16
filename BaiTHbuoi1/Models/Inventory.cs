using System;
using System.ComponentModel.DataAnnotations;

namespace BaiTHbuoi1.Models
{
    public class Inventory
    {
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        public int SupplierId { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        public DateTime ReceivedDate { get; set; } = DateTime.Now;

        public string? Notes { get; set; }

        // Navigation properties
        public virtual Product? Product { get; set; }
        public virtual Supplier? Supplier { get; set; }
    }
}