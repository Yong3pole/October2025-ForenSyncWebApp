using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;

namespace ForenSync_WebApp_New.Controllers
{
    public class DashboardController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            // Check if the modal has already been shown in this session
            var hasSeenModal = HttpContext.Session.GetString("HasSeenSyncModal") == "true";

            // If not seen, show it and set the flag
            ViewBag.ShowSyncModal = !hasSeenModal;
            HttpContext.Session.SetString("HasSeenSyncModal", "true");

            ViewData["Title"] = "Dashboard";
            return View("Index");
        }
    }
}