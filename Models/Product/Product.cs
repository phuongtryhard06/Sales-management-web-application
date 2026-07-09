using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FreshMart.Models
{
    public class Product
    {
        public int ProductId { get; set; }

        [Required]
        [StringLength(150)]
        public string? Name { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập giá bán")]
        [Range(0, 1000000000, ErrorMessage = "Giá bán không hợp lệ")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? DiscountPrice { get; set; }

        [StringLength(50)]
        public string? Weight { get; set; }   // e.g., "500g", "1L"

        public DateTime? ExpiryDate { get; set; }

        [Required]
        [Range(0, 100000, ErrorMessage = "Tồn kho không được nhỏ hơn 0")]
        public int Stock { get; set; }

        // Foreign key
        [Required]
        public int CategoryId { get; set; }

        // Navigation property
        public Category? Category { get; set; }

        [StringLength(255)]
        public string? ImagePath { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
