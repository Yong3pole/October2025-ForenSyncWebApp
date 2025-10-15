using ForenSync_WebApp_New.Data;
using ForenSync_WebApp_New.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace ForenSync_WebApp_New.Controllers
{
    public class SettingsController : Controller
    {
        private readonly ForenSyncDbContext _context;
        private readonly IWebHostEnvironment _environment;

        // Helper method to safely get string from database reader
        private string GetSafeString(object value)
        {
            if (value == null || value == DBNull.Value)
                return null;

            return value.ToString();
        }

        private DateTime ParseDateTimeSafe(object value)
        {
            if (value == null || value == DBNull.Value)
                return DateTime.Now;

            try
            {
                string dateString = value.ToString();

                // Handle different date formats
                if (dateString.Contains("T"))
                {
                    // ISO format: 2025-10-13T15:14:09.8677374+08:00
                    return DateTime.Parse(dateString);
                }
                else
                {
                    // Standard format: 2025-10-13 15:07:58
                    return DateTime.Parse(dateString);
                }
            }
            catch
            {
                return DateTime.Now; // Fallback to current time if parsing fails
            }
        }

        public SettingsController(ForenSyncDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public IActionResult Index()
        {
            return View();
        }

        // Add these methods to your SettingsController class:

        private bool DriveExists(string drivePath)
        {
            try
            {
                if (drivePath.Length >= 2 && drivePath[1] == ':')
                {
                    var driveLetter = drivePath[0].ToString().ToUpper();
                    return Directory.Exists(driveLetter + ":\\");
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        

        private async Task CopyDirectoryAsync(string sourceDir, string targetDir)
        {
            // Create target directory
            Directory.CreateDirectory(targetDir);

            // Copy all files
            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string targetFile = Path.Combine(targetDir, Path.GetFileName(file));
                using (var sourceStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
                using (var targetStream = new FileStream(targetFile, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
                {
                    await sourceStream.CopyToAsync(targetStream);
                }
            }

            // Recursively copy subdirectories
            foreach (string directory in Directory.GetDirectories(sourceDir))
            {
                string targetSubDir = Path.Combine(targetDir, Path.GetFileName(directory));
                await CopyDirectoryAsync(directory, targetSubDir);
            }
        }

        [HttpPost]
        public async Task<IActionResult> SelectVolume(string volumePath)
        {
            if (string.IsNullOrEmpty(volumePath))
            {
                return Json(new { success = false, error = "Please select a volume path." });
            }

            try
            {
                // Normalize the volume path
                volumePath = volumePath.TrimEnd('\\', '/');

                // Check if volume exists
                if (!Directory.Exists(volumePath) && !DriveExists(volumePath))
                {
                    return Json(new { success = false, error = $"Volume path '{volumePath}' does not exist or is not accessible." });
                }

                // Find the database file in root
                string sourceDbPath = Path.Combine(volumePath, "forensync.db");
                if (!System.IO.File.Exists(sourceDbPath))
                {
                    return Json(new { success = false, error = $"Could not find forensync.db in the root of the selected volume." });
                }

                // Perform the actual import (not just scanning)
                var importResult = await PerformActualImport(sourceDbPath, volumePath);

                return Json(importResult);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, error = $"Error during import: {ex.Message}" });
            }
        }

        private async Task<object> PerformActualImport(string sourceDbPath, string volumePath)
        {
            var messages = new List<string>
    {
        $"Starting import from: {sourceDbPath}",
        $"Volume: {volumePath}"
    };

            try
            {
                // Create connection to source database
                var sourceConnectionString = $"Data Source={sourceDbPath}";

                using (var sourceConnection = new SqliteConnection(sourceConnectionString))
                {
                    await sourceConnection.OpenAsync();

                    // Import case_logs
                    var caseLogsResult = await ImportCaseLogs(sourceConnection);
                    messages.Add($"Imported {caseLogsResult.ImportedCount} case log(s)");

                    // Import acquisition_log
                    var acquisitionLogsResult = await ImportAcquisitionLogs(sourceConnection);
                    messages.Add($"Imported {acquisitionLogsResult.ImportedCount} acquisition log(s)");

                    // Import audit_trail
                    var auditTrailsResult = await ImportAuditTrails(sourceConnection);
                    messages.Add($"Imported {auditTrailsResult.ImportedCount} audit trail(s)");

                    // Copy case folders for imported cases
                    var copyResult = await CopyCaseFolders(volumePath, caseLogsResult.ImportedCaseIds);
                    messages.Add($"Copied {copyResult.CopiedCount} case folder(s)");

                    // Build final result
                    return new
                    {
                        success = true,
                        volumePath,
                        databasePath = sourceDbPath,
                        importSummary = new
                        {
                            caseLogsImported = caseLogsResult.ImportedCount,
                            acquisitionLogsImported = acquisitionLogsResult.ImportedCount,
                            auditTrailsImported = auditTrailsResult.ImportedCount,
                            caseFoldersCopied = copyResult.CopiedCount,
                            caseIdsCopied = copyResult.CopiedCaseIds
                        },
                        messages = messages
                    };
                }
            }
            catch (Exception ex)
            {
                messages.Add($"Import error: {ex.Message}");
                return new
                {
                    success = false,
                    volumePath,
                    error = ex.Message,
                    messages = messages
                };
            }
        }

        private async Task<ImportResult> ImportCaseLogs(SqliteConnection sourceConnection)
        {
            var result = new ImportResult();

            try
            {
                // Get existing case IDs from local database
                var existingCaseIds = await _context.case_logs
                    .Select(c => c.case_id)
                    .ToListAsync();

                // Query source database for new case_logs
                var command = sourceConnection.CreateCommand();
                command.CommandText = "SELECT * FROM case_logs WHERE case_id NOT IN (" +
                                    string.Join(",", existingCaseIds.Select(id => $"'{id}'")) + ")";

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        try
                        {
                            var caseLog = new case_log
                            {
                                case_id = reader["case_id"]?.ToString(),
                                department = reader["department"]?.ToString(),
                                user_id = reader["user_id"]?.ToString(),
                                notes = reader["notes"]?.ToString(),
                                date = reader["date"] != DBNull.Value ? DateTime.Parse(reader["date"].ToString()) : DateTime.Now,
                                case_path = reader["case_path"]?.ToString()
                            };

                            if (!string.IsNullOrEmpty(caseLog.case_id))
                            {
                                _context.case_logs.Add(caseLog);
                                result.ImportedCaseIds.Add(caseLog.case_id);
                                result.ImportedCount++;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error importing case log: {ex.Message}");
                        }
                    }
                }

                if (result.ImportedCount > 0)
                {
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error importing case logs: {ex.Message}");
            }

            return result;
        }

        private async Task<ImportResult> ImportAcquisitionLogs(SqliteConnection sourceConnection)
        {
            var result = new ImportResult();

            try
            {
                // Get existing acquisition IDs from local database
                var existingAcquisitionIds = await _context.acquisition_log
                    .Select(a => a.acquisition_id)
                    .ToListAsync();

                // Query source database for new acquisition_logs
                var command = sourceConnection.CreateCommand();
                command.CommandText = "SELECT * FROM acquisition_log WHERE acquisition_id NOT IN (" +
                                    string.Join(",", existingAcquisitionIds) + ")";

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        try
                        {
                            var acquisitionLog = new acquisition_log
                            {
                                acquisition_id = Convert.ToInt32(reader["acquisition_id"]),
                                case_id = reader["case_id"]?.ToString(),
                                type = reader["type"]?.ToString(),
                                tool = reader["tool"]?.ToString(),
                                output_path = reader["output_path"]?.ToString(),
                                hash = reader["hash"]?.ToString(),
                                created_at = ParseDateTimeSafe(reader["created_at"]), // Parse to DateTime
                                entry_hash = reader["entry_hash"]?.ToString()
                            };

                            _context.acquisition_log.Add(acquisitionLog);
                            result.ImportedCount++;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error importing acquisition log: {ex.Message}");
                        }
                    }
                }

                if (result.ImportedCount > 0)
                {
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error importing acquisition logs: {ex.Message}");
            }

            return result;
        }

        private async Task<ImportResult> ImportAuditTrails(SqliteConnection sourceConnection)
        {
            var result = new ImportResult();

            try
            {
                // First check if audit_trail table exists
                var checkTableCommand = sourceConnection.CreateCommand();
                checkTableCommand.CommandText =
                    "SELECT name FROM sqlite_master WHERE type='table' AND name='audit_trail'";

                var tableExists = await checkTableCommand.ExecuteScalarAsync() != null;

                if (!tableExists)
                {
                    return result;
                }

                // Get existing event IDs from local database
                var existingEventIds = new List<int>();
                try
                {
                    existingEventIds = await _context.audit_trail
                        .Select(a => a.event_id)
                        .ToListAsync();
                }
                catch
                {
                    // If audit_trail table doesn't exist locally, import all
                }

                // Query source database for new audit_trail
                var command = sourceConnection.CreateCommand();
                command.CommandText = existingEventIds.Any()
                    ? "SELECT * FROM audit_trail WHERE event_id NOT IN (" +
                      string.Join(",", existingEventIds) + ")"
                    : "SELECT * FROM audit_trail";

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        try
                        {
                            var auditTrail = new audit_trail
                            {
                                event_id = Convert.ToInt32(reader["event_id"]),
                                user_id = reader["user_id"]?.ToString(),
                                action = reader["action"]?.ToString(),
                                created_at = GetSafeString(reader["created_at"]),
                                context = reader["context"]?.ToString(),
                                audit_hash = reader["audit_hash"]?.ToString()
                            };

                            _context.audit_trail.Add(auditTrail);
                            result.ImportedCount++;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error importing audit trail: {ex.Message}");
                        }
                    }
                }

                if (result.ImportedCount > 0)
                {
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error importing audit trails: {ex.Message}");
            }

            return result;
        }

        private async Task<CopyProgress> CopyDirectoryWithProgressAsync(string sourceDir, string targetDir, string caseId, int currentFolder, int totalFolders)
        {
            var progress = new CopyProgress();

            // Create target directory
            Directory.CreateDirectory(targetDir);

            // Get all files first to calculate total
            var allFiles = Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories);
            progress.TotalFiles = allFiles.Length;

            int currentFile = 0;

            foreach (string file in allFiles)
            {
                currentFile++;
                string relativePath = file.Substring(sourceDir.Length + 1);
                string targetFile = Path.Combine(targetDir, relativePath);

                // Create directory structure if needed
                string targetFileDir = Path.GetDirectoryName(targetFile);
                if (!Directory.Exists(targetFileDir))
                {
                    Directory.CreateDirectory(targetFileDir);
                }

                try
                {
                    // Try to copy with file sharing enabled
                    using (var sourceStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
                    using (var targetStream = new FileStream(targetFile, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
                    {
                        await sourceStream.CopyToAsync(targetStream);
                    }

                    progress.FilesCopied++;

                    // Log progress every 10 files or for large operations
                    if (currentFile % 10 == 0 || currentFile == progress.TotalFiles)
                    {
                        Console.WriteLine($"Progress: Case {currentFolder}/{totalFolders} - {caseId} - File {currentFile}/{progress.TotalFiles} - {Path.GetFileName(file)}");
                    }
                }
                catch (IOException ioEx) when (ioEx.Message.Contains("being used by another process"))
                {
                    // Skip locked files
                    Console.WriteLine($"Skipping locked file: {file}");
                    progress.SkippedFiles++;
                    continue;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error copying file {file}: {ex.Message}");
                    progress.Errors++;
                    continue;
                }
            }

            // Recursively handle subdirectories (already handled by SearchOption.AllDirectories)
            return progress;
        }
        private async Task<CopyResult> CopyCaseFolders(string volumePath, List<string> caseIds)
        {
            var result = new CopyResult();
            string sourceCasesPath = Path.Combine(volumePath, "Cases");
            string targetCasesPath = Path.Combine(_environment.WebRootPath, "Cases");

            // Create target Cases directory if it doesn't exist
            if (!Directory.Exists(targetCasesPath))
            {
                Directory.CreateDirectory(targetCasesPath);
            }

            // Progress tracking
            int totalFolders = caseIds.Count;
            int currentFolder = 0;

            foreach (string caseId in caseIds)
            {
                currentFolder++;
                try
                {
                    string sourceCasePath = Path.Combine(sourceCasesPath, caseId);
                    string targetCasePath = Path.Combine(targetCasesPath, caseId);

                    // Skip if source doesn't exist or target already exists
                    if (!Directory.Exists(sourceCasePath))
                    {
                        Console.WriteLine($"Source case folder not found: {sourceCasePath}");
                        continue;
                    }

                    if (Directory.Exists(targetCasePath))
                    {
                        Console.WriteLine($"Target case folder already exists, skipping: {targetCasePath}");
                        continue;
                    }

                    // Copy the entire case folder with progress
                    var copyProgress = await CopyDirectoryWithProgressAsync(sourceCasePath, targetCasePath, caseId, currentFolder, totalFolders);

                    result.CopiedCount++;
                    result.CopiedCaseIds.Add(caseId);

                    Console.WriteLine($"Successfully copied case folder: {caseId} ({copyProgress.FilesCopied} files)");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error copying case folder {caseId}: {ex.Message}");
                }
            }

            return result;
        }



        // Update the helper classes
        public class ImportResult
        {
            public int ImportedCount { get; set; }
            public List<string> ImportedCaseIds { get; set; } = new List<string>();
        }

        public class CopyResult
        {
            public int CopiedCount { get; set; }
            public List<string> CopiedCaseIds { get; set; } = new List<string>();
        }




    }
}