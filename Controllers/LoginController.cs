using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
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
        public async Task<IActionResult> Authenticate(string username, string password)
        {
            var user = _context.users_tbl
                .FirstOrDefault(u => u.user_id == username);

            if (user != null)
            {
                // Check password
                if (user.password == password)
                {
                    // Check if user is active
                    if (!user.active)
                    {
                        ViewBag.Error = "Your account is deactivated. Please contact administrator.";
                        return View("Index");
                    }

                    // User is active and password correct - proceed with login
                    HttpContext.Session.SetString("UserId", user.user_id);
                    HttpContext.Session.SetString("FirstName", user.firstName);
                    HttpContext.Session.SetString("LastName", user.lastName);
                    HttpContext.Session.SetString("Role", user.role);

                    var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.user_id),
                new Claim(ClaimTypes.Role, user.role),
                new Claim("FullName", $"{user.firstName} {user.lastName}")
            };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

                    return RedirectToAction("Index", "Dashboard");
                }
                else
                {
                    // Password is wrong
                    ViewBag.Error = "Invalid password. Please try again.";
                    return View("Index");
                }
            }

            // User not found
            ViewBag.Error = "Username not found. Please try again.";
            return View("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            // Clear session (if you're using it)
            HttpContext.Session.Clear();

            // Sign out from authentication
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction("Index", "Login");
        }
    }
}