using System.ComponentModel.DataAnnotations;

namespace ForenSync_WebApp_New.Models
{
    public class audit_trail
    {
        [Key]
        public int event_id { get; set; }  // Changed to int

        public string user_id { get; set; }

        public string action { get; set; }

        public string created_at { get; set; }  // Changed to string (was timestamp)

        public string context { get; set; }  // Changed from details to context

        public string audit_hash { get; set; }  // Changed from ip_address to audit_hash
    }
}