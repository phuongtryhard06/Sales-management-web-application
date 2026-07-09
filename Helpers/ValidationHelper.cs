using System.Text.RegularExpressions;

namespace FreshMart.Helpers
{
    /// <summary>
    /// Input validation and sanitization helper
    /// </summary>
    public static class ValidationHelper
    {
        /// <summary>
        /// Validates if an email is in valid format
        /// </summary>
        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Validates password strength
        /// </summary>
        public static (bool IsValid, string Message) ValidatePassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return (false, "Password cannot be empty");

            if (password.Length < 6)
                return (false, "Password must be at least 6 characters long");

            if (password.Length > 50)
                return (false, "Password cannot exceed 50 characters");

            return (true, "Password is valid");
        }

        /// <summary>
        /// Validates full name format
        /// </summary>
        public static bool IsValidFullName(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return false;

            if (fullName.Length < 3 || fullName.Length > 40)
                return false;

            return Regex.IsMatch(fullName, @"^[A-Za-z\s]+$");
        }

        /// <summary>
        /// Sanitizes HTML input to prevent XSS attacks
        /// </summary>
        public static string SanitizeInput(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            // Remove potentially dangerous characters and HTML tags
            return Regex.Replace(input, @"[<>""'%;()&+\-]", "").Trim();
        }

        /// <summary>
        /// Validates numeric range
        /// </summary>
        public static bool IsValidPrice(decimal price)
        {
            return price >= 0 && price <= 1000000000;
        }

        /// <summary>
        /// Validates stock quantity
        /// </summary>
        public static bool IsValidStock(int stock)
        {
            return stock >= 0 && stock <= 100000;
        }

        /// <summary>
        /// Validates product name length
        /// </summary>
        public static bool IsValidProductName(string name)
        {
            return !string.IsNullOrWhiteSpace(name) && name.Length <= 150;
        }

        /// <summary>
        /// Validates file extension for images
        /// </summary>
        public static bool IsValidImageFile(string fileName)
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var extension = Path.GetExtension(fileName).ToLower();
            return allowedExtensions.Contains(extension);
        }
    }
}
