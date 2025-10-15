using System;
using System.ComponentModel.DataAnnotations;

namespace ForenSync_WebApp_New.Models
{
    public class case_log
    {
        [Key]
        public string case_id { get; set; }

        public string department { get; set; }
        public string user_id { get; set; }
        public string notes { get; set; }
        public DateTime date { get; set; }
        public string case_path { get; set; }
    }
}