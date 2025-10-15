using System.ComponentModel.DataAnnotations;

namespace ForenSync_WebApp_New.Models
{
    public class ImportToMainLog
    {
        [Key]
        public int import_id { get; set; }
        public string import_timestamp { get; set; }
        public int case_logs_imported { get; set; }
        public int acquisition_logs_imported { get; set; }
        public int audit_trails_imported { get; set; }
        public string imported_case_ids { get; set; } // JSON as text
        public string status { get; set; }
        public string error_message { get; set; }
    }
}