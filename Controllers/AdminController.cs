using ForenSync_WebApp_New.Data;
using ForenSync_WebApp_New.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Globalization;

namespace ForenSync_WebApp_New.Controllers
{
    [Authorize(Roles = "admin")]  // ← Only admin role
    public class AdminController : Controller
    {
        private readonly ForenSyncDbContext _context;

        public AdminController(ForenSyncDbContext context)
        {
            _context = context;
        }

        ////////////////////////////////////////// MANAGER USERS or VIEW USERS //////////////////////////////////////////

        // GET: /Admin/ManageUsers
        public IActionResult ManageUsers(string searchString)
        {
            // Check if user is admin
            var userRole = HttpContext.Session.GetString("Role");
            if (string.IsNullOrEmpty(userRole) || userRole.ToLower() != "admin")
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            var users = _context.users_tbl.AsQueryable();

            // Apply search filter if provided
            if (!string.IsNullOrEmpty(searchString))
            {
                users = users.Where(u =>
                    (u.firstName != null && u.firstName.Contains(searchString)) ||
                    (u.lastName != null && u.lastName.Contains(searchString)) ||
                    (u.email != null && u.email.Contains(searchString)) ||
                    (u.department != null && u.department.Contains(searchString)) ||
                    (u.role != null && u.role.Contains(searchString)));
            }

            // Map to ViewModel with NULL handling
            var userViewModels = users.Select(u => new UserViewModel
            {
                UserId = u.user_id ?? "N/A",
                FirstName = u.firstName ?? "Not set",
                LastName = u.lastName ?? "Not set",
                Email = u.email ?? "Not set",
                Phone = u.phone ?? "Not set",
                Department = u.department ?? "Not set",
                BadgeNum = u.badge_num ?? "Not set", // Add this line
                Role = u.role ?? "Not set",
                IsActive = u.active,
                CreatedAt = u.created_at
            }).ToList();

            ViewBag.SearchString = searchString;
            return View(userViewModels);
        }

        // Other admin actions remain the same...
        public IActionResult SessionLogs()
        {
            var userRole = HttpContext.Session.GetString("Role");
            if (string.IsNullOrEmpty(userRole) || userRole.ToLower() != "admin")
            {
                return RedirectToAction("AccessDenied", "Home");
            }
            return View();
        }

        public IActionResult ExportLogs()
        {
            var userRole = HttpContext.Session.GetString("Role");
            if (string.IsNullOrEmpty(userRole) || userRole.ToLower() != "admin")
            {
                return RedirectToAction("AccessDenied", "Home");
            }
            return View();
        }

        public async Task<IActionResult> ImportHistory(string searchTerm, string status, string startDate, string endDate)
        {
            var userRole = HttpContext.Session.GetString("Role");
            if (string.IsNullOrEmpty(userRole) || userRole.ToLower() != "admin")
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            try
            {
                var query = _context.import_to_main_logs.AsQueryable();

                // Apply search filter
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    query = query.Where(i =>
                        i.import_id.Contains(searchTerm) ||
                        i.status.Contains(searchTerm) ||
                        (i.error_message != null && i.error_message.Contains(searchTerm)) ||
                        (i.imported_case_ids != null && i.imported_case_ids.Contains(searchTerm))
                    );
                }

                // Apply status filter
                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(i => i.status.ToUpper() == status.ToUpper());
                }

                // Apply date filter - ULTRA SIMPLE STRING COMPARISON
                if (!string.IsNullOrEmpty(startDate))
                {
                    query = query.Where(i => !string.IsNullOrEmpty(i.import_timestamp) &&
                                            i.import_timestamp.Substring(0, 10).CompareTo(startDate) >= 0);
                }

                if (!string.IsNullOrEmpty(endDate))
                {
                    query = query.Where(i => !string.IsNullOrEmpty(i.import_timestamp) &&
                                            i.import_timestamp.Substring(0, 10).CompareTo(endDate) <= 0);
                }

                var importLogs = await query
                    .OrderByDescending(i => i.import_timestamp)
                    .ToListAsync();

                return View(importLogs);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading import history: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return View(new List<import_to_main_logs>());
            }
        }

        ////////////////////////////////////////// ADD USER //////////////////////////////////////////

        // GET: /Admin/AddUser
        public IActionResult AddUser()
        {
            // Check if user is admin
            var userRole = HttpContext.Session.GetString("Role");
            if (string.IsNullOrEmpty(userRole) || userRole.ToLower() != "admin")
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            return View();
        }

        // POST: /Admin/AddUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddUser(AddUserViewModel model)
        {
            // Check if user is admin
            var userRole = HttpContext.Session.GetString("Role");
            if (string.IsNullOrEmpty(userRole) || userRole.ToLower() != "admin")
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Validate role against allowed values
            var allowedRoles = new[] { "admin", "operator" };
            if (!allowedRoles.Contains(model.Role?.ToLower()))
            {
                ModelState.AddModelError("Role", "Invalid role selected. Please choose either 'Admin' or 'Operator'.");
                return View(model);
            }

            // Check if user ID already exists
            var existingUser = _context.users_tbl.FirstOrDefault(u => u.user_id == model.UserId);
            if (existingUser != null)
            {
                ModelState.AddModelError("UserId", "User ID already exists. Please choose a different one.");
                return View(model);
            }

            // Check if email already exists
            var existingEmail = _context.users_tbl.FirstOrDefault(u => u.email == model.Email);
            if (existingEmail != null)
            {
                ModelState.AddModelError("Email", "Email address already exists. Please use a different email.");
                return View(model);
            }

            // Check if badge number already exists (if provided)
            if (!string.IsNullOrEmpty(model.BadgeNum))
            {
                var existingBadge = _context.users_tbl.FirstOrDefault(u => u.badge_num == model.BadgeNum);
                if (existingBadge != null)
                {
                    ModelState.AddModelError("BadgeNum", "Badge number already exists. Please use a different badge number.");
                    return View(model);
                }
            }

            try
            {
                // Create new user
                var newUser = new users_tbl
                {
                    user_id = model.UserId,
                    firstName = model.FirstName,
                    lastName = model.LastName,
                    email = model.Email,
                    phone = model.Phone,
                    department = model.Department,
                    badge_num = model.BadgeNum, // Add badge number
                    role = model.Role,
                    password = model.Password, // Note: We'll hash this later
                    active = model.IsActive,
                    created_at = DateTime.Now,
                    created_by = HttpContext.Session.GetString("UserId") ?? "system"
                };

                _context.users_tbl.Add(newUser);
                _context.SaveChanges();

                TempData["SuccessMessage"] = $"User '{model.FirstName} {model.LastName}' added successfully!";
                return RedirectToAction("ManageUsers");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"An error occurred while adding the user: {ex.Message}");
                return View(model);
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////// EDIT USER //////////////////////////////////////////

        // GET: /Admin/EditUser/{id}
        public IActionResult EditUser(string id)  // Change parameter name to 'id'
        {
            // Check if user is admin
            var userRole = HttpContext.Session.GetString("Role");
            if (string.IsNullOrEmpty(userRole) || userRole.ToLower() != "admin")
            {
                TempData["ErrorMessage"] = "Access denied. Admin privileges required.";
                return RedirectToAction("ManageUsers");
            }

            if (string.IsNullOrEmpty(id))  // Now using 'id'
            {
                TempData["ErrorMessage"] = "User ID is required.";
                return RedirectToAction("ManageUsers");
            }

            var user = _context.users_tbl.FirstOrDefault(u => u.user_id == id);  // Now using 'id'
            if (user == null)
            {
                TempData["ErrorMessage"] = $"User with ID '{id}' not found.";
                return RedirectToAction("ManageUsers");
            }

            // Map to EditUserViewModel
            var model = new EditUserViewModel
            {
                UserId = user.user_id,
                FirstName = user.firstName ?? "",
                LastName = user.lastName ?? "",
                Email = user.email ?? "",
                Phone = user.phone ?? "",
                Department = user.department ?? "",
                BadgeNum = user.badge_num ?? "",
                Role = user.role ?? "",
                IsActive = user.active
            };

            return View(model);
        }

        // POST: /Admin/EditUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditUser(EditUserViewModel model)
        {
            // Check if user is admin
            var userRole = HttpContext.Session.GetString("Role");
            if (string.IsNullOrEmpty(userRole) || userRole.ToLower() != "admin")
            {
                return RedirectToAction("AccessDenied", "Home");
            }

            // Remove password validation errors if ResetPassword is false
            if (!model.ResetPassword)
            {
                ModelState.Remove("NewPassword");
                ModelState.Remove("ConfirmNewPassword");
            }

            // Manual password validation if ResetPassword is true
            if (model.ResetPassword)
            {
                if (string.IsNullOrEmpty(model.NewPassword))
                {
                    ModelState.AddModelError("NewPassword", "New password is required when resetting password.");
                }
                else
                {
                    // Use the PasswordStrength validation manually
                    var passwordStrengthAttribute = new PasswordStrengthAttribute();
                    var passwordValidationResult = passwordStrengthAttribute.GetValidationResult(model.NewPassword, new ValidationContext(model));
                    if (passwordValidationResult != ValidationResult.Success)
                    {
                        ModelState.AddModelError("NewPassword", passwordValidationResult.ErrorMessage);
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = _context.users_tbl.FirstOrDefault(u => u.user_id == model.UserId);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("ManageUsers");
            }

            // Check if email already exists (excluding current user)
            var existingEmail = _context.users_tbl.FirstOrDefault(u => u.email == model.Email && u.user_id != model.UserId);
            if (existingEmail != null)
            {
                ModelState.AddModelError("Email", "Email address already exists. Please use a different email.");
                return View(model);
            }

            // Check if badge number already exists (excluding current user, and if provided)
            if (!string.IsNullOrEmpty(model.BadgeNum))
            {
                var existingBadge = _context.users_tbl.FirstOrDefault(u => u.badge_num == model.BadgeNum && u.user_id != model.UserId);
                if (existingBadge != null)
                {
                    ModelState.AddModelError("BadgeNum", "Badge number already exists. Please use a different badge number.");
                    return View(model);
                }
            }

            try
            {
                // Update user fields
                user.firstName = model.FirstName;
                user.lastName = model.LastName;
                user.email = model.Email;
                user.phone = model.Phone;
                user.department = model.Department;
                user.badge_num = model.BadgeNum;
                user.role = model.Role;
                user.active = model.IsActive;

                // Update password only if reset is requested and new password is provided
                if (model.ResetPassword && !string.IsNullOrEmpty(model.NewPassword))
                {
                    user.password = model.NewPassword; // Note: We'll hash this later
                }

                _context.SaveChanges();

                TempData["SuccessMessage"] = $"User '{model.FirstName} {model.LastName}' updated successfully!";
                return RedirectToAction("ManageUsers");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"An error occurred while updating the user: {ex.Message}");
                return View(model);
            }
        }

    }
}