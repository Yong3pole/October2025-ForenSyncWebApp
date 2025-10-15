namespace ForenSync_WebApp_New.Models
{
    public class CopyProgress
    {
        public int TotalFiles { get; set; }
        public int FilesCopied { get; set; }
        public int SkippedFiles { get; set; }
        public int Errors { get; set; }
    }

    // Update CopyResult class to include file counts:
    public class CopyResult
    {
        public int CopiedCount { get; set; }
        public List<string> CopiedCaseIds { get; set; } = new List<string>();
        public int TotalFilesProcessed { get; set; }
        public int SkippedFiles { get; set; }
    }
}
