using System;
using System.ComponentModel.DataAnnotations;

namespace ForenSync_WebApp_New.Models
{
    public class ImportLog
    {
        [Key]
        public int import_id { get; set; }

        public string user_id { get; set; }

        public string volume_path { get; set; }

        public string status { get; set; } // Success, Partial, Failed, NoData

        public int cases_imported { get; set; }

        public int acquisitions_imported { get; set; }

        public int audit_trails_imported { get; set; }

        public int folders_copied { get; set; }

        public string details { get; set; }

        public string error_message { get; set; }

        public DateTime imported_at { get; set; }

        public string import_hash { get; set; } // For integrity checking
    }
}