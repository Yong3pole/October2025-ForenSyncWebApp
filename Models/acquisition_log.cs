using System;
using System.ComponentModel.DataAnnotations;

namespace ForenSync_WebApp_New.Models
{
    public class acquisition_log
    {
        [Key]
        public int acquisition_id { get; set; }

        public string case_id { get; set; }
        public string type { get; set; }
        public string tool { get; set; }
        public string output_path { get; set; }
        public string hash { get; set; }
        public DateTime created_at { get; set; }

        public string entry_hash { get; set; }
  
    }
}