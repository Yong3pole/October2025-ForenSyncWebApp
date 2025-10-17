using ForenSync_WebApp_New.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ForenSync_WebApp_New.Controllers
{
    [Authorize]  // ← Anyone who is logged in
    public class UserController : Controller
    {
        private readonly ForenSyncDbContext _context;

        public UserController(ForenSyncDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult User_Management()
        {
            var users = _context.users_tbl.ToList(); // adjust if your table name differs
            return View(users);
        }
    }

}