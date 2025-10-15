using System;
using System.ComponentModel.DataAnnotations;

namespace ForenSync_WebApp_New.Models
{
    public class AcquisitionLogViewModel
    {
        public string AcquisitionId { get; set; }

        [Display(Name = "Case ID")]
        public string CaseId { get; set; }

        public string Type { get; set; }

        public string Tool { get; set; }

        [Display(Name = "Output Path")]
        public string OutputPath { get; set; }

        public string Hash { get; set; }

        [Display(Name = "Created At")]
        public string CreatedAt { get; set; } // Keep as string for formatting flexibility

        [Display(Name = "Entry Hash")]
        public string EntryHash { get; set; }
    }
}