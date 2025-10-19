using ForenSync_WebApp_New.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
        public async Task<IActionResult> GetDashboardData(int months = 6)
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

                // Apply date filter for acquisitions
                var acquisitionQuery = _context.acquisition_log.AsQueryable();
                var caseQuery = _context.case_logs.AsQueryable();
                var importQuery = _context.import_to_main_logs.AsQueryable();

                if (months > 0)
                {
                    var dateFilter = DateTime.Now.AddMonths(-months);
                    acquisitionQuery = acquisitionQuery.Where(a => EF.Property<DateTime>(a, "created_at") >= dateFilter);
                    caseQuery = caseQuery.Where(c => EF.Property<DateTime>(c, "date") >= dateFilter);
                    importQuery = importQuery.Where(i => EF.Property<DateTime>(i, "import_timestamp") >= dateFilter);
                }

                // Group by type and count using LINQ
                var typeCounts = await acquisitionQuery
                    .GroupBy(a => EF.Property<string>(a, "type"))
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
                var totalCases = await acquisitionQuery
                    .Select(a => EF.Property<string>(a, "case_id"))
                    .Distinct()
                    .CountAsync();

                var totalImports = await importQuery.CountAsync(); // Count from import_to_main_logs table
                var totalExports = 0; // Adjust if you have export data
                var totalAnomalies = 0; // Adjust if you have anomalies data

                // Get timeline data based on selected months
                DateTime startDate;
                string periodFormat;
                List<string> monthLabels;
                List<int> casesData;
                List<int> acquisitionsData;

                if (months == 1) // Last 30 days - show daily data
                {
                    startDate = DateTime.Now.AddDays(-30);
                    periodFormat = "MMM dd";

                    // Get cases by day
                    var casesByDay = await caseQuery
                        .Where(c => EF.Property<DateTime>(c, "date") >= startDate)
                        .GroupBy(c => EF.Property<DateTime>(c, "date").Date)
                        .Select(g => new { Date = g.Key, Count = g.Count() })
                        .OrderBy(x => x.Date)
                        .ToListAsync();

                    // Get acquisitions by day
                    var acquisitionsByDay = await acquisitionQuery
                        .Where(a => EF.Property<DateTime>(a, "created_at") >= startDate)
                        .GroupBy(a => EF.Property<DateTime>(a, "created_at").Date)
                        .Select(g => new { Date = g.Key, Count = g.Count() })
                        .OrderBy(x => x.Date)
                        .ToListAsync();

                    // Generate labels and data for the last 30 days
                    monthLabels = new List<string>();
                    casesData = new List<int>();
                    acquisitionsData = new List<int>();

                    for (int i = 29; i >= 0; i--)
                    {
                        var date = DateTime.Now.AddDays(-i).Date;
                        monthLabels.Add(date.ToString(periodFormat));

                        var casesCount = casesByDay.FirstOrDefault(c => c.Date == date)?.Count ?? 0;
                        casesData.Add(casesCount);

                        var acquisitionsCount = acquisitionsByDay.FirstOrDefault(a => a.Date == date)?.Count ?? 0;
                        acquisitionsData.Add(acquisitionsCount);
                    }
                }
                else // Monthly data
                {
                    int displayMonths = months > 0 ? months : 12; // Default to 12 months if "All Time"
                    startDate = DateTime.Now.AddMonths(-displayMonths);
                    periodFormat = "MMM yyyy";

                    // Get cases by month
                    var casesByMonth = await caseQuery
                        .Where(c => EF.Property<DateTime>(c, "date") >= startDate)
                        .GroupBy(c => new {
                            Year = EF.Property<DateTime>(c, "date").Year,
                            Month = EF.Property<DateTime>(c, "date").Month
                        })
                        .Select(g => new
                        {
                            Year = g.Key.Year,
                            Month = g.Key.Month,
                            Count = g.Count()
                        })
                        .OrderBy(x => x.Year)
                        .ThenBy(x => x.Month)
                        .ToListAsync();

                    // Get acquisitions by month
                    var acquisitionsByMonth = await acquisitionQuery
                        .Where(a => EF.Property<DateTime>(a, "created_at") >= startDate)
                        .GroupBy(a => new {
                            Year = EF.Property<DateTime>(a, "created_at").Year,
                            Month = EF.Property<DateTime>(a, "created_at").Month
                        })
                        .Select(g => new
                        {
                            Year = g.Key.Year,
                            Month = g.Key.Month,
                            Count = g.Count()
                        })
                        .OrderBy(x => x.Year)
                        .ThenBy(x => x.Month)
                        .ToListAsync();

                    // Generate labels and data
                    monthLabels = new List<string>();
                    casesData = new List<int>();
                    acquisitionsData = new List<int>();

                    for (int i = displayMonths - 1; i >= 0; i--)
                    {
                        var date = DateTime.Now.AddMonths(-i);
                        monthLabels.Add(date.ToString(periodFormat));

                        var casesCount = casesByMonth
                            .FirstOrDefault(c => c.Year == date.Year && c.Month == date.Month)?.Count ?? 0;
                        casesData.Add(casesCount);

                        var acquisitionsCount = acquisitionsByMonth
                            .FirstOrDefault(a => a.Year == date.Year && a.Month == date.Month)?.Count ?? 0;
                        acquisitionsData.Add(acquisitionsCount);
                    }
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
                        acquisitions = acquisitionsData.ToArray(),
                        period = months == 1 ? "daily" : "monthly"
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
                    recentActivity = new object[] { },
                    dateRange = months
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