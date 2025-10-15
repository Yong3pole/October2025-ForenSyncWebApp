namespace ForenSync_WebApp_New.Models
{
    public class ScanResult
    {
        public int Count { get; set; }
        public List<string> CaseIds { get; set; } = new List<string>();
        public object DebugInfo { get; set; } // Add this line
    }
}
