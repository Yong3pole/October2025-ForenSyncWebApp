using System.ComponentModel.DataAnnotations;

namespace ForenSync_WebApp_New.Models
{
    public class ChangePasswordViewModel
    {
        [Required]
        public string CurrentPassword { get; set; }

        [Required]
        [PasswordStrength]
        [NotEqual("CurrentPassword", ErrorMessage = "New password must be different from current password.")]
        public string NewPassword { get; set; }

        [Required]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
        public string ConfirmNewPassword { get; set; }
    }

    // Custom validation attribute
    public class NotEqualAttribute : ValidationAttribute
    {
        private readonly string _otherProperty;

        public NotEqualAttribute(string otherProperty)
        {
            _otherProperty = otherProperty;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var otherPropertyInfo = validationContext.ObjectType.GetProperty(_otherProperty);

            if (otherPropertyInfo == null)
            {
                return new ValidationResult($"Unknown property: {_otherProperty}");
            }

            var otherPropertyValue = otherPropertyInfo.GetValue(validationContext.ObjectInstance, null);

            if (value != null && value.Equals(otherPropertyValue))
            {
                return new ValidationResult(ErrorMessage ?? $"This field must not be equal to {_otherProperty}.");
            }

            return ValidationResult.Success;
        }
    }
}