using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace ForenSync_WebApp_New.Models
{
    public class AddUserViewModel
    {
        [Required(ErrorMessage = "User ID is required")]
        [Display(Name = "User ID")]
        public string UserId { get; set; }

        [Required(ErrorMessage = "First name is required")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Last name is required")]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; }

        [Phone(ErrorMessage = "Invalid phone number")]
        public string Phone { get; set; }

        [Display(Name = "Department")]
        public string Department { get; set; }

        [Display(Name = "Badge Number")]
        public string BadgeNum { get; set; }

        [Required(ErrorMessage = "Role is required")]
        public string Role { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [PasswordStrength] // Use your custom attribute here
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Confirm password is required")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; }

        [Display(Name = "Active User")]
        public bool IsActive { get; set; } = true;
    }

    public class PasswordStrengthAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var password = value as string;

            if (string.IsNullOrWhiteSpace(password))
                return new ValidationResult("Password is required.");

            bool hasUpper = Regex.IsMatch(password, "[A-Z]");
            bool hasLower = Regex.IsMatch(password, "[a-z]");
            bool hasDigit = Regex.IsMatch(password, "[0-9]");
            bool hasSymbol = Regex.IsMatch(password, "[^A-Za-z0-9]");
            bool isLongEnough = password.Length >= 8;

            if (hasUpper && hasLower && hasDigit && hasSymbol && isLongEnough)
                return ValidationResult.Success;

            return new ValidationResult("Password must be at least 8 characters long and include uppercase, lowercase, number, and symbol.");
        }
    }
}