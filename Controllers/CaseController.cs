using ForenSync_WebApp_New.Data;
using ForenSync_WebApp_New.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ForenSync_WebApp_New.Controllers
{
    [Authorize]  // ← Anyone who is logged in
    public class CaseController : Controller
    {
        private readonly ForenSyncDbContext _context;

        public CaseController(ForenSyncDbContext context)
        {
            _context = context;
        }

        public IActionResult CaseViewer(string searchTerm, string startDate, string endDate)
        {
            var query = _context.case_logs.AsQueryable();

            // Search filter
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(c =>
                    c.department.Contains(searchTerm) ||
                    c.user_id.Contains(searchTerm) ||
                    c.notes.Contains(searchTerm) ||
                    c.case_path.Contains(searchTerm));
            }

            // ✅ DATE RANGE FILTER - USING OUR SUCCESSFUL PATTERN
            if (!string.IsNullOrEmpty(startDate))
            {
                if (DateTime.TryParse(startDate, out DateTime startDateParsed))
                {
                    query = query.Where(c => c.date >= startDateParsed);
                }
            }

            if (!string.IsNullOrEmpty(endDate))
            {
                if (DateTime.TryParse(endDate, out DateTime endDateParsed))
                {
                    // Add one day to include the entire end date
                    var endDateInclusive = endDateParsed.AddDays(1);
                    query = query.Where(c => c.date < endDateInclusive);
                }
            }

            var cases = query.OrderByDescending(c => c.date).ToList();
            return View(cases);
        }


        // GET: /Case/AcquisitionHistory
        public IActionResult AcquisitionHistory(string searchTerm, string filterType, DateTime? startDate, DateTime? endDate)
        {
            var acquisitionLogs = _context.acquisition_log.AsQueryable();

            // Apply search filter if provided
            if (!string.IsNullOrEmpty(searchTerm))
            {
                acquisitionLogs = acquisitionLogs.Where(a =>
                    a.case_id.Contains(searchTerm) ||
                    a.acquisition_id.Contains(searchTerm) ||
                    a.type.Contains(searchTerm) ||
                    a.tool.Contains(searchTerm));
            }

            // Apply type filter if provided
            if (!string.IsNullOrEmpty(filterType))
            {
                acquisitionLogs = acquisitionLogs.Where(a => a.type.ToLower() == filterType.ToLower());
            }

            // Apply date range filter if provided
            if (startDate.HasValue)
            {
                acquisitionLogs = acquisitionLogs.Where(a => a.created_at >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                // Add one day to include the entire end date
                var endDateInclusive = endDate.Value.AddDays(1);
                acquisitionLogs = acquisitionLogs.Where(a => a.created_at < endDateInclusive);
            }

            // Map to ViewModel - simplified for the view (removed OutputPath, Hash, EntryHash)
            var viewModels = acquisitionLogs
                .OrderByDescending(a => a.created_at)
                .Select(a => new AcquisitionLogViewModel
                {
                    AcquisitionId = a.acquisition_id,
                    CaseId = a.case_id ?? "N/A",
                    Type = a.type ?? "Not set",
                    Tool = a.tool ?? "Not set",
                    CreatedAt = a.created_at.ToString("yyyy-MM-dd HH:mm") // Simplified format
                }).ToList();

            return View(viewModels);
        }

    }
}
