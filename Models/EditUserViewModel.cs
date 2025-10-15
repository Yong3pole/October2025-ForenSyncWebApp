using System.ComponentModel.DataAnnotations;

namespace ForenSync_WebApp_New.Models
{
    public class EditUserViewModel
    {
        // User ID is read-only for display purposes
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

        [Display(Name = "Active User")]
        public bool IsActive { get; set; }

        [Display(Name = "Reset Password")]
        public bool ResetPassword { get; set; }

        // Only validate password strength if ResetPassword is true
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm New Password")]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
        public string ConfirmNewPassword { get; set; }
    }
}