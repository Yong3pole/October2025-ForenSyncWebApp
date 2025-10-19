using System;
using System.ComponentModel.DataAnnotations;

namespace ForenSync_WebApp_New.Models
{
    public class MftAnalysisReport
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string ReportId { get; set; }

        [Required]
        [MaxLength(100)]
        public string CaseId { get; set; }

        [Required]
        [MaxLength(100)]
        public string UserId { get; set; }

        [Required]
        [MaxLength(255)]
        public string FileName { get; set; }

        public long FileSize { get; set; }

        [Required]
        [MaxLength(500)]
        public string ReportPath { get; set; }

        [Range(0, 100)]
        public decimal AnalysisScore { get; set; }

        [Required]
        [MaxLength(20)]
        public string AlertLevel { get; set; }

        [Required]
        [MaxLength(20)]
        public string AlertColor { get; set; }

        public int TotalFiles { get; set; }
        public int LargeTimestampDiscrepancies { get; set; }
        public decimal LargeTimestampPercentage { get; set; }
        public int RapidDeletions { get; set; }
        public decimal RapidDeletionPercentage { get; set; }

        public string AiInsights { get; set; }
        public string TopSuspiciousFiles { get; set; }

        public DateTime DateCreated { get; set; }
        public DateTime DateModified { get; set; }
    }
}