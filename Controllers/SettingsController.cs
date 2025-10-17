using ForenSync_WebApp_New.Data;
using ForenSync_WebApp_New.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace ForenSync_WebApp_New.Controllers
{
    [Authorize]  // ← Anyone who is logged in
    public class SettingsController : Controller
    {
        private readonly ForenSyncDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public SettingsController(ForenSyncDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

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

        public IActionResult Index()
        {
            // Get recent imports for the view
            var recentImports = _context.import_to_main_logs
                .OrderByDescending(i => i.import_timestamp)
                .Take(10)
                .ToList();

            return View(recentImports);
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

            // Generate import ID and timestamp
            string importId = Guid.NewGuid().ToString();
            string importTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // Initialize counters
            int caseLogsImported = 0;
            int acquisitionLogsImported = 0;
            int auditTrailsImported = 0;
            List<string> importedCaseIds = new List<string>();
            string status = "SUCCESS";
            string errorMessage = "N/A";

            try
            {
                // Create connection to source database
                var sourceConnectionString = $"Data Source={sourceDbPath}";

                using (var sourceConnection = new SqliteConnection(sourceConnectionString))
                {
                    await sourceConnection.OpenAsync();

                    // Import case_logs
                    var caseLogsResult = await ImportCaseLogs(sourceConnection);
                    caseLogsImported = caseLogsResult.ImportedCount;
                    importedCaseIds = caseLogsResult.ImportedCaseIds;
                    messages.Add($"Imported {caseLogsImported} case log(s)");

                    // Import acquisition_log
                    var acquisitionLogsResult = await ImportAcquisitionLogs(sourceConnection);
                    acquisitionLogsImported = acquisitionLogsResult.ImportedCount;
                    messages.Add($"Imported {acquisitionLogsImported} acquisition log(s)");

                    // Import audit_trail
                    var auditTrailsResult = await ImportAuditTrails(sourceConnection);
                    auditTrailsImported = auditTrailsResult.ImportedCount;
                    messages.Add($"Imported {auditTrailsImported} audit trail(s)");

                    // SKIPPED: Copy case folders - only importing logs
                    messages.Add($"Skipped copying case folders (log import only)");

                    // Log successful import to database
                    await LogImportToDatabase(importId, importTimestamp, caseLogsImported,
                        acquisitionLogsImported, auditTrailsImported, importedCaseIds, status, errorMessage);

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
                            caseFoldersCopied = 0, // Now always 0
                            caseIdsCopied = new List<string>() // Empty list
                        },
                        messages = messages
                    };
                }
            }
            catch (Exception ex)
            {
                // Update status for error case
                status = "FAILED";
                errorMessage = ex.Message;

                messages.Add($"Import error: {ex.Message}");

                // Log failed import to database
                await LogImportToDatabase(importId, importTimestamp, caseLogsImported,
                    acquisitionLogsImported, auditTrailsImported, importedCaseIds, status, errorMessage);

                return new
                {
                    success = false,
                    volumePath,
                    error = ex.Message,
                    messages = messages
                };
            }
        }

        // IMPORT CASE LOGS , ACQUISITION LOGS, AUDIT TRAILS //
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
                                acquisition_id = reader["acquisition_id"]?.ToString(),
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
                var existingEventIds = new List<string>();
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
                                event_id = reader["event_id"]?.ToString(),
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

        // Add this method to log to import_to_main_logs table
        private async Task LogImportToDatabase(string importId, string importTimestamp,
            int caseLogsImported, int acquisitionLogsImported, int auditTrailsImported,
            List<string> importedCaseIds, string status, string errorMessage)
        {
            try
            {
                // Convert importedCaseIds list to JSON string
                string importedCaseIdsJson = JsonSerializer.Serialize(importedCaseIds);

                var importLog = new import_to_main_logs
                {
                    import_id = importId,
                    import_timestamp = importTimestamp,
                    case_logs_imported = caseLogsImported,
                    acquisition_logs_imported = acquisitionLogsImported,
                    audit_trails_imported = auditTrailsImported,
                    imported_case_ids = importedCaseIdsJson, // Store as JSON string
                    status = status,
                    error_message = errorMessage
                };

                _context.import_to_main_logs.Add(importLog);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Log the error but don't break the main import process
                Console.WriteLine($"Failed to log import to database: {ex.Message}");
            }
        }

        // Update the helper classes
        public class ImportResult
        {
            public int ImportedCount { get; set; }
            public List<string> ImportedCaseIds { get; set; } = new List<string>();
        }
    }
}