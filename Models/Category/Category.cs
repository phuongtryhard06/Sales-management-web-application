using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace FreshMart.Models
{
    public class Category
    {
        public int CategoryId { get; set; }

        [Required]
        [StringLength(100)]
        public string? CategoryName { get; set; }

        [StringLength(255)]
        public string? Description { get; set; }

        // Navigation property
        public ICollection<Product>? Products { get; set; }
    }
}
