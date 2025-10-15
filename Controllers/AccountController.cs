using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Linq;
using ForenSync_WebApp_New.Data;
using ForenSync_WebApp_New.Models;

namespace ForenSync_WebApp_New.Controllers
{
    public class AccountController : Controller
    {
        private readonly ForenSyncDbContext _context;

        public AccountController(ForenSyncDbContext context)
        {
            _context = context;
        }

        // GET: /Account/EditProfile
        public IActionResult EditProfile()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Auth");

            var user = _context.users_tbl.FirstOrDefault(u => u.user_id == userId);
            if (user == null)
                return NotFound();

            // Map to ViewModel
            var model = new EditProfileViewModel
            {
                FirstName = user.firstName,
                LastName = user.lastName,
                Email = user.email,
                Phone = user.phone
            };

            return View(model);
        }

        // POST: /Account/EditProfile
        [HttpPost]
        public IActionResult EditProfile(EditProfileViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Auth");

            var user = _context.users_tbl.FirstOrDefault(u => u.user_id == userId);
            if (user == null)
                return NotFound();

            // Update only the allowed fields
            user.firstName = model.FirstName;
            user.lastName = model.LastName;
            user.email = model.Email;
            user.phone = model.Phone;

            _context.SaveChanges();
            TempData["SuccessMessage"] = "Profile updated successfully.";
            return RedirectToAction("EditProfile");
        }

        // GET: /Account/ChangePassword
        public IActionResult ChangePassword()
        {
            return View();
        }

        // POST: /Account/ChangePassword
        [HttpPost]
        public IActionResult ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var userId = HttpContext.Session.GetString("UserId");
            var user = _context.users_tbl.FirstOrDefault(u => u.user_id == userId);

            if (user == null || user.password != model.CurrentPassword)
            {
                ModelState.AddModelError("", "Current password is incorrect.");
                return View(model);
            }

            // Additional check to ensure new password is different from current password
            if (model.NewPassword == model.CurrentPassword)
            {
                ModelState.AddModelError("NewPassword", "New password must be different from current password.");
                return View(model);
            }

            user.password = model.NewPassword;
            _context.SaveChanges();
            TempData["SuccessMessage"] = "Password changed successfully.";
            return RedirectToAction("ChangePassword");
        }

    }
}
