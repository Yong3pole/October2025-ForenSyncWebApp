using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System;

namespace YourProjectName.Controllers
{
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

        [HttpPost]
        public IActionResult AnalyzeMFT(IFormFile csvFile)
        {
            try
            {
                if (csvFile == null || csvFile.Length == 0)
                {
                    return Json(new { success = false, error = "Please select a valid CSV file." });
                }

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
                    message = "MFT analysis request received successfully"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = ex.Message });
            }
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
    }
}