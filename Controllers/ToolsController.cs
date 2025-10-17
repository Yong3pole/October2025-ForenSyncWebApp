using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Json; // Add this for PostAsJsonAsync
using System.Text;


namespace YourProjectName.Controllers
{
    [Authorize]  // ← Anyone who is logged in
    public class ToolsController : Controller
    {
        // Existing MemoryPane action
        public IActionResult MemoryPane()
        {
            return View();
        }

        // New AnomalyViewer action
        public IActionResult AnomalyViewer()
        {
            return View();
        }

        private readonly HttpClient _httpClient;

        public ToolsController(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        [HttpPost]
        public async Task<IActionResult> AnalyzeMFT(IFormFile csvFile)
        {
            try
            {
                if (csvFile == null || csvFile.Length == 0)
                {
                    return Json(new { success = false, error = "Please select a valid CSV file." });
                }

                // Read and analyze the MFT CSV
                var analysisResult = AnalyzeMftCsv(csvFile);

                // NEW: Get AI Insights from Mistral
                var aiAnalysis = await GetMistralAnalysis(analysisResult);

                // Get user_id from session
                var userId = HttpContext.Session.GetString("UserId") ?? "Unknown";

                // Extract case_id from the actual folder structure
                var caseId = ExtractCaseIdFromActualPath(csvFile);

                // Generate proper report path
                var reportPath = $"{caseId}\\Evidence\\Cloned Drive\\{csvFile.FileName}";

                return Json(new
                {
                    success = true,
                    case_id = caseId,
                    user_id = userId,
                    date_created = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    report_path = reportPath,
                    file_name = csvFile.FileName,
                    file_size = csvFile.Length,

                    // ADD AI Analysis to your response
                    ai_insights = aiAnalysis,

                    // MFT Analysis Results
                    analysis = new
                    {
                        score = analysisResult.Score,
                        alert_level = analysisResult.AlertLevel,
                        alert_color = analysisResult.AlertColor,
                        metrics = new
                        {
                            total_files = analysisResult.Metrics.TotalFiles,
                            large_timestamp_discrepancies = analysisResult.Metrics.LargeTimestampDiscrepancies,
                            large_timestamp_percentage = analysisResult.Metrics.LargeTimestampPercentage,
                            rapid_deletions = analysisResult.Metrics.RapidDeletions,
                            rapid_deletion_percentage = analysisResult.Metrics.RapidDeletionPercentage
                        },
                        top_suspicious_files = analysisResult.TopSuspiciousFiles
                    },
                    message = "MFT analysis completed successfully"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
        }

        private async Task<string> GetMistralAnalysis(MftAnalysisResult analysisResult)
        {
            try
            {
                // Create summary for Mistral
                var summary = $@"
MFT Forensic Findings:
- Score: {analysisResult.Score}/100
- Alert Level: {analysisResult.AlertLevel}
- Total Files: {analysisResult.Metrics.TotalFiles}
- Timestamp Anomalies: {analysisResult.Metrics.LargeTimestampDiscrepancies}
- Rapid Deletions: {analysisResult.Metrics.RapidDeletions}
- Top Suspicious: {string.Join(", ", analysisResult.TopSuspiciousFiles)}

As a cybercrime investigator, what does this pattern suggest and what should we investigate next?
";

                var requestData = new
                {
                    model = "mistral",
                    prompt = summary,
                    stream = false
                };

                // Use the injected _httpClient
                var response = await _httpClient.PostAsJsonAsync(
                    "http://localhost:11434/api/generate",
                    requestData
                );

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<OllamaResponse>();
                    return result?.Response ?? "AI analysis unavailable";
                }

                return "Unable to get AI analysis - Ollama service may not be running";
            }
            catch (Exception ex)
            {
                return $"AI analysis error: {ex.Message}";
            }
        }

        // Response model
        public class OllamaResponse
        {
            public string Response { get; set; } = string.Empty;
        }

        private MftAnalysisResult AnalyzeMftCsv(IFormFile csvFile)
        {
            var records = new List<MftRecord>();

            using var stream = csvFile.OpenReadStream();
            using var reader = new StreamReader(stream);

            // Read header to get column mapping
            var headerLine = reader.ReadLine();
            if (headerLine == null) return new MftAnalysisResult();

            var headers = ParseCsvLine(headerLine);
            var fileNameIndex = headers.IndexOf("FileName");
            var created10Index = headers.IndexOf("Created0x10");
            var created30Index = headers.IndexOf("Created0x30");
            var lastModified10Index = headers.IndexOf("LastModified0x10");

            // Validate that we have the required columns
            if (fileNameIndex == -1 || created10Index == -1 || created30Index == -1 || lastModified10Index == -1)
            {
                throw new Exception("CSV file is missing required columns (FileName, Created0x10, Created0x30, LastModified0x10)");
            }

            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                if (string.IsNullOrEmpty(line)) continue;

                var fields = ParseCsvLine(line);
                if (fields.Count > Math.Max(fileNameIndex, Math.Max(created10Index, Math.Max(created30Index, lastModified10Index))))
                {
                    var record = new MftRecord
                    {
                        FileName = fields[fileNameIndex],
                        Created0x10 = ParseDateTime(fields[created10Index]),
                        Created0x30 = ParseDateTime(fields[created30Index]),
                        LastModified0x10 = ParseDateTime(fields[lastModified10Index])
                    };

                    // Calculate CreatedDelta (SI Created - FN Created)
                    if (record.Created0x10 != DateTime.MinValue && record.Created0x30 != DateTime.MinValue)
                    {
                        record.CreatedDelta = (record.Created0x10 - record.Created0x30).TotalSeconds;
                    }

                    // Calculate Lifespan (LastModified - Created)
                    if (record.LastModified0x10 != DateTime.MinValue && record.Created0x10 != DateTime.MinValue)
                    {
                        record.Lifespan = (record.LastModified0x10 - record.Created0x10).TotalSeconds;
                    }

                    records.Add(record);
                }
            }

            // Filter out system files
            var userRecords = records.Where(r =>
                !string.IsNullOrEmpty(r.FileName) &&
                !r.FileName.StartsWith("$")).ToList();

            // Calculate metrics
            var totalFiles = userRecords.Count;
            var largeDeltas = userRecords.Count(r => Math.Abs(r.CreatedDelta) > 60 * 60 * 24 * 365);
            var largePct = totalFiles > 0 ? (double)largeDeltas / totalFiles * 100 : 0;
            var rapidDeletions = userRecords.Count(r => r.Lifespan >= 0 && r.Lifespan <= 60);
            var rapidPct = totalFiles > 0 ? (double)rapidDeletions / totalFiles * 100 : 0;

            // Calculate score
            var score = CalculateAnomalyScore(userRecords, totalFiles, largeDeltas, largePct, rapidDeletions, rapidPct);

            // Get top suspicious files
            var topSuspicious = userRecords
                .OrderByDescending(r => Math.Abs(r.CreatedDelta))
                .Take(5)
                .Select(r => new {
                    file_name = r.FileName?.Length > 35 ? r.FileName.Substring(0, 35) : r.FileName ?? "Unknown",
                    years_delta = Math.Round(Math.Abs(r.CreatedDelta) / (60.0 * 60 * 24 * 365), 1)
                })
                .ToList();

            return new MftAnalysisResult
            {
                Score = Math.Round(score, 1),
                AlertLevel = score < 40 ? "Low" : score < 70 ? "Medium" : "High",
                AlertColor = score < 40 ? "green" : score < 70 ? "orange" : "red",
                Metrics = new MftMetrics
                {
                    TotalFiles = totalFiles,
                    LargeTimestampDiscrepancies = largeDeltas,
                    LargeTimestampPercentage = Math.Round(largePct, 1),
                    RapidDeletions = rapidDeletions,
                    RapidDeletionPercentage = Math.Round(rapidPct, 1)
                },
                TopSuspiciousFiles = topSuspicious
            };
        }

        private double CalculateAnomalyScore(List<MftRecord> records, int totalFiles,
            int largeDeltas, double largePct, int rapidDeletions, double rapidPct)
        {
            if (totalFiles == 0) return 0;

            double score = 0;

            // 1. Large timestamp discrepancies (30 points max)
            score += Math.Min(largePct * 4, 30);

            // 2. Rapid deletions (30 points max) - reduced weight
            score += Math.Min(rapidPct * 0.5, 30);

            // 3. Executable files with anomalies (25 points max)
            var exeAnomalies = records.Count(r =>
                (r.FileName?.ToLower().Contains(".exe") == true ||
                 r.FileName?.ToLower().Contains(".dll") == true ||
                 r.FileName?.ToLower().Contains(".bat") == true ||
                 r.FileName?.ToLower().Contains(".ps1") == true) &&
                Math.Abs(r.CreatedDelta) > 60);

            var exePct = (double)exeAnomalies / totalFiles * 100;
            score += Math.Min(exePct * 10, 25);

            // 4. Very large batch operations (15 points max)
            var batchOperations = records
                .GroupBy(r => r.Created0x10)
                .Where(g => g.Count() > 100)
                .Count();

            score += Math.Min(batchOperations * 3, 15);

            return Math.Min(score, 100);
        }

        // Helper method to parse CSV line
        private List<string> ParseCsvLine(string line)
        {
            var fields = new List<string>();
            var currentField = new StringBuilder();
            bool inQuotes = false;

            foreach (char c in line)
            {
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    fields.Add(currentField.ToString().Trim());
                    currentField.Clear();
                }
                else
                {
                    currentField.Append(c);
                }
            }

            fields.Add(currentField.ToString().Trim()); // Add last field
            return fields;
        }

        // Helper method to parse datetime strings
        private DateTime ParseDateTime(string dateTimeStr)
        {
            if (string.IsNullOrEmpty(dateTimeStr)) return DateTime.MinValue;

            if (DateTime.TryParse(dateTimeStr, out DateTime result))
            {
                return result;
            }

            return DateTime.MinValue;
        }

        private string ExtractCaseIdFromActualPath(IFormFile file)
        {
            // Try to get the full path from the file (may be limited by browser security)
            var fullPath = file.FileName;

            // Look for CASE_ pattern in the path
            var casePattern = @"CASE_\d{8}_\d{6}";
            var match = System.Text.RegularExpressions.Regex.Match(fullPath, casePattern);

            if (match.Success)
            {
                return match.Value;
            }

            // If we can't get the full path, try to extract from filename
            return ExtractCaseIdFromFolderStructure(file.FileName);
        }

        private string ExtractCaseIdFromFolderStructure(string fileName)
        {
            // Extract the timestamp number (14 digits) from filename
            var timestampPattern = @"(\d{14})"; // Matches 20251013154315
            var match = System.Text.RegularExpressions.Regex.Match(fileName, timestampPattern);

            if (match.Success)
            {
                // Always add "CASE_" prefix to the timestamp
                return "CASE_" + match.Value;
            }

            // Fallback patterns
            var fallbackPatterns = new[]
            {
                @"(CASE_\d{8}_\d{6})",  // CASE_20251013_232321
                @"(CASE-\d{4}-\d{3})"   // CASE-2024-001
            };

            foreach (var pattern in fallbackPatterns)
            {
                var patternMatch = System.Text.RegularExpressions.Regex.Match(fileName, pattern);
                if (patternMatch.Success)
                    return patternMatch.Value;
            }

            // Final fallback
            return "CASE_UNKNOWN";
        }

        private string GenerateReportPath(string caseId, string fileName)
        {
            // Generate path like: CASE_20251013_232321\Evidence\Cloned Drive
            return $"{caseId}\\Evidence\\Cloned Drive";
        }

        // Model classes
        public class MftRecord
        {
            public string FileName { get; set; }
            public DateTime Created0x10 { get; set; }
            public DateTime Created0x30 { get; set; }
            public DateTime LastModified0x10 { get; set; }
            public double CreatedDelta { get; set; }
            public double Lifespan { get; set; }
        }

        public class MftAnalysisResult
        {
            public double Score { get; set; }
            public string AlertLevel { get; set; }
            public string AlertColor { get; set; }
            public MftMetrics Metrics { get; set; }
            public object TopSuspiciousFiles { get; set; }
        }

        public class MftMetrics
        {
            public int TotalFiles { get; set; }
            public int LargeTimestampDiscrepancies { get; set; }
            public double LargeTimestampPercentage { get; set; }
            public int RapidDeletions { get; set; }
            public double RapidDeletionPercentage { get; set; }
        }
    }
}