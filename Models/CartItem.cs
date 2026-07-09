using System.ComponentModel.DataAnnotations;

namespace FreshMart.Models
{
    /// <summary>
    /// Shopping cart item with validation
    /// </summary>
    public class CartItem
    {
        [Required(ErrorMessage = "Product ID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Invalid product ID")]
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Product name is required")]
        [StringLength(150, ErrorMessage = "Product name cannot exceed 150 characters")]
        public string ProductName { get; set; } = string.Empty;

        public string? Name { get; set; }

        public string? ImagePath { get; set; }

        [Range(0.01, 100000, ErrorMessage = "Price must be between 0.01 and 100000")]
        public decimal Price { get; set; }

        [Range(1, 100, ErrorMessage = "Quantity must be between 1 and 100")]
        public int Quantity { get; set; } = 1;
    }
}
