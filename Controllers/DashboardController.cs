using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ForenSync_WebApp_New.Data;
using Microsoft.EntityFrameworkCore;

namespace ForenSync_WebApp_New.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ForenSyncDbContext _context;

        public DashboardController(ForenSyncDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var hasSeenModal = HttpContext.Session.GetString("HasSeenSyncModal") == "true";
            ViewBag.ShowSyncModal = !hasSeenModal;
            HttpContext.Session.SetString("HasSeenSyncModal", "true");
            ViewData["Title"] = "Dashboard";
            return View("Index");
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboardData()
        {
            try
            {
                // Query for acquisition types from acquisition_log table using Entity Framework
                var acquisitionTypes = new Dictionary<string, int>
        {
            { "Drive image/Clone", 0 },
            { "Memory Capture", 0 },
            { "Snapshots", 0 }
        };

                // Group by type and count using LINQ
                var typeCounts = await _context.acquisition_log
                    .GroupBy(a => a.type)
                    .Select(g => new { Type = g.Key, Count = g.Count() })
                    .ToListAsync();

                foreach (var item in typeCounts)
                {
                    var dbType = item.Type.ToLower();

                    // Map database types to our categories
                    if (dbType.Contains("drive"))
                    {
                        acquisitionTypes["Drive image/Clone"] += item.Count;
                    }
                    else if (dbType.Contains("memory") || dbType.Contains("capture"))
                    {
                        acquisitionTypes["Memory Capture"] += item.Count;
                    }
                    else if (dbType.Contains("snapshot"))
                    {
                        acquisitionTypes["Snapshots"] += item.Count;
                    }
                    else
                    {
                        // Log any unknown types for debugging
                        Console.WriteLine($"Unknown acquisition type: {dbType}");
                    }
                }

                // Get other dashboard statistics using Entity Framework
                var totalCases = await _context.acquisition_log
                    .Select(a => a.case_id)
                    .Distinct()
                    .CountAsync();

                var totalImports = await _context.acquisition_log.CountAsync();
                var totalExports = 0; // Adjust if you have export data
                var totalAnomalies = 0; // Adjust if you have anomalies data

                // Get timeline data for the last 6 months
                var sixMonthsAgo = DateTime.Now.AddMonths(-6);

                // Get cases by month from case_logs table
                var casesByMonth = await _context.case_logs
                    .Where(c => c.date >= sixMonthsAgo)
                    .GroupBy(c => new { Year = c.date.Year, Month = c.date.Month })
                    .Select(g => new
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        Count = g.Count()
                    })
                    .OrderBy(x => x.Year)
                    .ThenBy(x => x.Month)
                    .ToListAsync();

                // Get acquisitions by month from acquisition_log table
                var acquisitionsByMonth = await _context.acquisition_log
                    .Where(a => a.created_at >= sixMonthsAgo)
                    .GroupBy(a => new { Year = a.created_at.Year, Month = a.created_at.Month })
                    .Select(g => new
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        Count = g.Count()
                    })
                    .OrderBy(x => x.Year)
                    .ThenBy(x => x.Month)
                    .ToListAsync();

                // Generate labels for the last 6 months
                var monthLabels = new List<string>();
                var casesData = new List<int>();
                var acquisitionsData = new List<int>();

                for (int i = 5; i >= 0; i--)
                {
                    var date = DateTime.Now.AddMonths(-i);
                    var monthName = date.ToString("MMM");
                    monthLabels.Add(monthName);

                    // Find cases count for this month
                    var casesCount = casesByMonth
                        .FirstOrDefault(c => c.Year == date.Year && c.Month == date.Month)?.Count ?? 0;
                    casesData.Add(casesCount);

                    // Find acquisitions count for this month
                    var acquisitionsCount = acquisitionsByMonth
                        .FirstOrDefault(a => a.Year == date.Year && a.Month == date.Month)?.Count ?? 0;
                    acquisitionsData.Add(acquisitionsCount);
                }

                var chartData = new
                {
                    acquisitionTypes = new
                    {
                        labels = acquisitionTypes.Where(x => x.Value > 0).Select(x => x.Key).ToArray(),
                        data = acquisitionTypes.Where(x => x.Value > 0).Select(x => x.Value).ToArray()
                    },
                    activityTimeline = new
                    {
                        labels = monthLabels.ToArray(),
                        cases = casesData.ToArray(),
                        acquisitions = acquisitionsData.ToArray()
                    }
                };

                return Json(new
                {
                    success = true,
                    totalCases,
                    totalImports,
                    totalExports,
                    totalAnomalies,
                    charts = chartData,
                    recentActivity = new object[] { }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetDashboardData: {ex.Message}");
                return Json(new { success = false, error = ex.Message });
            }
        }
    }
}