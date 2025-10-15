using System.ComponentModel.DataAnnotations;

namespace ForenSync_WebApp_New.Models
{
    public class users_tbl
    {
        [Key]
        public string user_id { get; set; }

        [Required]
        public string password { get; set; }

        [Required]
        public string role { get; set; }

        [Required(ErrorMessage = "First name is required")]
        [Display(Name = "First Name")]
        public string firstName { get; set; }

        [Required(ErrorMessage = "Last name is required")]
        [Display(Name = "Last Name")]
        public string lastName { get; set; }

        public string department { get; set; }

        public string badge_num { get; set; }

        public DateTime created_at { get; set; }

        public string created_by { get; set; }

        public bool active { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string email { get; set; }

        [Phone(ErrorMessage = "Invalid phone number")]
        public string phone { get; set; }
    }
}