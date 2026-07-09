using System.ComponentModel.DataAnnotations;

namespace FreshMart.Models
{
    public class CheckoutViewModel : IValidatableObject
    {
        public int UserId { get; set; }

        // FULL NAME
        [Display(Name = "Họ và tên")]
        [Required(ErrorMessage = "Vui lòng nhập họ và tên.")]
        [StringLength(40, MinimumLength = 3, ErrorMessage = "Họ và tên phải từ 3 đến 40 ký tự.")]
        public string FullName { get; set; } = string.Empty;

        // EMAIL
        [Display(Name = "Email")]
        [Required(ErrorMessage = "Vui lòng nhập email.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        public string Email { get; set; } = string.Empty;

        // ADDRESS
        [Display(Name = "Địa chỉ")]
        [Required(ErrorMessage = "Vui lòng nhập địa chỉ nhận hàng.")]
        [MinLength(5, ErrorMessage = "Địa chỉ phải ít nhất 5 ký tự.")]
        public string Address { get; set; } = string.Empty;

        // PHONE
        [Display(Name = "Số điện thoại")]
        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Số điện thoại phải gồm 10 chữ số.")]
        public string Phone { get; set; } = string.Empty;

        // MÃ BƯU CHÍNH (VIỆT NAM: 5-6 chữ số)
        [Display(Name = "Mã bưu điện")]
        [Required(ErrorMessage = "Vui lòng nhập mã bưu chính.")]
        [RegularExpression(@"^\d{5,6}$",
            ErrorMessage = "Mã bưu chính gồm 5-6 chữ số (ví dụ: 70000).")]
        public string PostalCode { get; set; } = string.Empty;

        // PAYMENT METHOD
        [Display(Name = "Phương thức thanh toán")]
        [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán.")]
        public string PaymentMethod { get; set; } = string.Empty;

        // CARD NUMBER (ONLY FOR CREDIT / DEBIT)
        [Display(Name = "Số thẻ")]
        [RegularExpression(@"^\d{16}$", ErrorMessage = "Số thẻ phải gồm 16 chữ số.")]
        public string? CardNumber { get; set; }

        // EXPIRY MM/YY
        [Display(Name = "Ngày hết hạn (MM/YY)")]
        [RegularExpression(@"^(0[1-9]|1[0-2])\/\d{2}$", ErrorMessage = "Định dạng ngày hết hạn phải là MM/YY.")]
        public string? Expiry { get; set; }

        // CVV
        [Display(Name = "Mã CVV")]
        [RegularExpression(@"^\d{3}$", ErrorMessage = "Mã CVV phải gồm 3 chữ số.")]
        public string? CVV { get; set; }

        // 🔐 CONDITIONAL SERVER-SIDE VALIDATION
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (PaymentMethod == "Credit Card" || PaymentMethod == "Debit Card")
            {
                if (string.IsNullOrWhiteSpace(CardNumber))
                    yield return new ValidationResult("Vui lòng nhập số thẻ.", new[] { nameof(CardNumber) });

                if (string.IsNullOrWhiteSpace(Expiry))
                    yield return new ValidationResult("Vui lòng nhập ngày hết hạn.", new[] { nameof(Expiry) });

                if (string.IsNullOrWhiteSpace(CVV))
                    yield return new ValidationResult("Vui lòng nhập mã CVV.", new[] { nameof(CVV) });
            }
        }
    }
}