using Microsoft.AspNetCore.Mvc;
using ForenSync_WebApp_New.Data;
using System.Linq;

namespace ForenSync_WebApp_New.Controllers
{
    public class LoginController : Controller
    {
        private readonly ForenSyncDbContext _context;

        public LoginController(ForenSyncDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            ViewData["Title"] = "Login";
            return View();
        }

        [HttpPost]
        public IActionResult Authenticate(string username, string password)
        {
            var user = _context.users_tbl
                .FirstOrDefault(u => u.user_id == username && u.password == password);

            if (user != null)
            {
                HttpContext.Session.SetString("UserId", user.user_id);
                HttpContext.Session.SetString("FirstName", user.firstName);
                HttpContext.Session.SetString("LastName", user.lastName);
                HttpContext.Session.SetString("Role", user.role);

                return RedirectToAction("Index", "Dashboard");
            }

            ViewBag.Error = "Invalid credentials. Please try again.";
            return View("Index");
        }

        [HttpPost]
        public IActionResult Logout() // LOGOUT BUTTON -- find it in the debar partial view
        {
            // Clear all session keys
            HttpContext.Session.Clear();

            // Optional: redirect to login page
            return RedirectToAction("Index", "Login");
        }
    }
}