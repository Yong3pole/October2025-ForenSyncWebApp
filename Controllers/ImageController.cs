using Microsoft.AspNetCore.Mvc;

namespace ForenSync_WebApp_New.Controllers
{
    public class ImageController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            ViewData["Title"] = "Image Management";

            // Later: fetch images from DB and pass to view
            return View("Image_Management");
        }
    }
}