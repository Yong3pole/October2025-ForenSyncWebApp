using System;
using System.ComponentModel.DataAnnotations;

namespace ForenSync_WebApp_New.Models
{
    public class AcquisitionLogViewModel
    {
        public string? AcquisitionId { get; set; }
        public string? CaseId { get; set; }
        public string? Type { get; set; }
        public string? Tool { get; set; }
        public string? CreatedAt { get; set; }
        public string OutputPath { get; set; } // ADD THIS PROPERTY
        // Removed: OutputPath, Hash, EntryHash since they're hidden in the view
    }
}