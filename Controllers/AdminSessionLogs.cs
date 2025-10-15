using ForenSync_WebApp_New.Data;
using ForenSync_WebApp_New.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace ForenSync_WebApp_New.Controllers
{
    public class AdminSessionLogsController : Controller
    {
        private readonly ForenSyncDbContext _context;

        public AdminSessionLogsController(ForenSyncDbContext context)
        {
            _context = context;
        }

        public IActionResult SessionLogs(string searchTerm = "", string filterAction = "")
        {

            // Check if user is admin
            var userRole = HttpContext.Session.GetString("Role");
            if (string.IsNullOrEmpty(userRole) || userRole.ToLower() != "admin")
            {
                return RedirectToAction("AccessDenied", "Home");
            }
            var query = _context.audit_trail.AsQueryable();

            // Apply search filter
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(log =>
                    log.user_id.Contains(searchTerm) ||
                    log.action.Contains(searchTerm) ||
                    log.context.Contains(searchTerm));
            }

            // Apply action filter
            if (!string.IsNullOrEmpty(filterAction))
            {
                query = query.Where(log => log.action == filterAction);
            }

            var logs = query.OrderByDescending(log => log.event_id).ToList();

            ViewBag.SearchTerm = searchTerm;
            ViewBag.FilterAction = filterAction;

            return View(logs);
        }
    }
}