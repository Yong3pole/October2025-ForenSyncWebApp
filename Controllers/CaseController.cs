using ForenSync_WebApp_New.Data;
using ForenSync_WebApp_New.Models;
using Microsoft.AspNetCore.Mvc;

namespace ForenSync_WebApp_New.Controllers
{
    public class CaseController : Controller
    {
        private readonly ForenSyncDbContext _context;

        public CaseController(ForenSyncDbContext context)
        {
            _context = context;
        }

        public IActionResult CaseViewer(string searchTerm, DateTime? filterDate)
        {
            var query = _context.case_logs.AsQueryable();

            if (filterDate.HasValue)
            {
                query = query.Where(c => c.date.Date == filterDate.Value.Date);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(c =>
                    c.department.Contains(searchTerm) ||
                    c.user_id.Contains(searchTerm) ||
                    c.notes.Contains(searchTerm) ||
                    c.case_path.Contains(searchTerm));
            }

            var results = query.OrderByDescending(c => c.date).ToList();
            return View(results);
        }


        // GET: /Case/History
        public IActionResult AcquisitionHistory(string searchTerm, string filterType)
        {
            var acquisitionLogs = _context.acquisition_log.AsQueryable();

            // Apply search filter if provided
            if (!string.IsNullOrEmpty(searchTerm))
            {
                acquisitionLogs = acquisitionLogs.Where(a =>
                    a.case_id.Contains(searchTerm) ||
                    a.type.Contains(searchTerm) ||
                    a.tool.Contains(searchTerm) ||
                    a.output_path.Contains(searchTerm));
            }

            // Apply type filter if provided
            if (!string.IsNullOrEmpty(filterType))
            {
                acquisitionLogs = acquisitionLogs.Where(a => a.type == filterType);
            }

            // Map to ViewModel - fix the CreatedAt mapping for non-nullable DateTime
            var viewModels = acquisitionLogs.Select(a => new AcquisitionLogViewModel
            {
                AcquisitionId = a.acquisition_id,
                CaseId = a.case_id ?? "N/A",
                Type = a.type ?? "Not set",
                Tool = a.tool ?? "Not set",
                OutputPath = a.output_path ?? "Not set",
                Hash = a.hash ?? "Not set",
                CreatedAt = a.created_at.ToString("yyyy-MM-dd HH:mm:ss"), // Simple ToString for non-nullable DateTime
                EntryHash = a.entry_hash ?? "Not set"
            }).ToList();

            ViewBag.SearchTerm = searchTerm;
            ViewBag.FilterType = filterType;
            return View(viewModels);
        }

    }
}
