//using System.ComponentModel.DataAnnotations;

//namespace FreshMart.Models
//{
//    public class User
//    {
//        public int UserId { get; set; }

//        [Required]
//        public string FullName { get; set; } = string.Empty;

//        [Required, EmailAddress]
//        public string Email { get; set; } = string.Empty;

//        public string PasswordHash { get; set; } = string.Empty;

//        public string Role { get; set; } = "Customer";

//        // FIX FOR ERROR:
//        public DateTime CreatedAt { get; set; } = DateTime.Now;
//    }

//}


//using System.ComponentModel.DataAnnotations;
//using System.ComponentModel.DataAnnotations.Schema;

//namespace FreshMart.Models
//{
//    public class User
//    {
//        public int UserId { get; set; }

//        // FULL NAME
//        [Required(ErrorMessage = "Full name is required.")]
//        [StringLength(40, MinimumLength = 3, ErrorMessage = "Full name must be between 3 and 40 characters.")]
//        [RegularExpression(@"^[A-Za-z\s]+$", ErrorMessage = "Full name can contain only letters.")]
//        public string FullName { get; set; } = string.Empty;

//        // EMAIL
//        [Required(ErrorMessage = "Email is required.")]
//        [RegularExpression(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.(com|ca|net|org|edu|gov|info|io)$",
//     ErrorMessage = "Enter a valid email address like example@gmail.com.")]
//        public string Email { get; set; } = string.Empty;


//        // PASSWORD
//        [Required(ErrorMessage = "Password is required.")]
//        [StringLength(20, MinimumLength = 8, ErrorMessage = "Password must be 8–20 characters long.")]
//        [RegularExpression(@"^(?=.*[A-Z])(?=.*[a-z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$",
//            ErrorMessage = "Password must include uppercase, lowercase, number, and symbol.")]
//        public string PasswordHash { get; set; } = string.Empty;

//        // CONFIRM PASSWORD (NOT STORED IN DB)
//        [NotMapped]
//        [Required(ErrorMessage = "Please confirm your password.")]
//        [Compare("PasswordHash", ErrorMessage = "Passwords do not match.")]
//        public string ConfirmPassword { get; set; } = string.Empty;


//        // ROLE
//        [Required(ErrorMessage = "Please select a user role.")]
//        public string Role { get; set; } = "Customer";

//        // CREATED AT
//        public DateTime CreatedAt { get; set; } = DateTime.Now;
//    }
//}


using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FreshMart.Models
{
    public class User
    {
        public int UserId { get; set; }

        // FULL NAME
        [Required(ErrorMessage = "Full name is required.")]
        [StringLength(40, MinimumLength = 3, ErrorMessage = "Full name must be between 3 and 40 characters.")]
        [RegularExpression(@"^[A-Za-zÀ-ỹ\s]+$", ErrorMessage = "Họ tên chỉ được chứa chữ cái.")]
        public string FullName { get; set; } = string.Empty;

        // EMAIL
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        public string Email { get; set; } = string.Empty;


        // PASSWORD (USER ENTERS THIS)
        [NotMapped]
        [Required(ErrorMessage = "Password is required.")]
        [StringLength(50, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long.")]
        public string Password { get; set; } = string.Empty;


        // CONFIRM PASSWORD (NOT STORED IN DB)
        [NotMapped]
        [Required(ErrorMessage = "Please confirm your password.")]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;


        // STORED HASH (SHA256 Base64 = 44 characters)
        [StringLength(500)]
        public string PasswordHash { get; set; } = string.Empty;


        // ROLE
        [Required(ErrorMessage = "Please select a user role.")]
        public string Role { get; set; } = "Customer";

        // CREATED TIME
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // USER AVATAR
        public string? AvatarPath { get; set; }
    }
}
