using System.ComponentModel.DataAnnotations;

namespace ForenSync_WebApp_New.Models
{
    public class UserViewModel
    {
        public string UserId { get; set; }

        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        public string Email { get; set; }
        public string Phone { get; set; }

        [Display(Name = "Department")]
        public string Department { get; set; }

        [Display(Name = "Badge Number")]
        public string BadgeNum { get; set; }

        public string Role { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; }

        [Display(Name = "Created At")]
        public DateTime CreatedAt { get; set; }
    }
}