using System.ComponentModel.DataAnnotations;

namespace ForenSync_WebApp_New.Models
{
    public class SessionLog
    {
        [Key]
        public int session_id { get; set; }
        public string session_timestamp { get; set; }
        public string user_id { get; set; }
        public string action_type { get; set; }
    }
}