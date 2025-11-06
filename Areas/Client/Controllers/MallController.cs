using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;

namespace ABCDMall.Controllers
{
    [Area("Client")]
    public class MallController : Controller
    {
        // Sample notifications data (replace with DB: Tbl_Notification later)
        private static readonly List<string> Notifications = new()
        {
            "🎉 Zara grand opening - 2nd floor",
            "🍔 20% off at KFC (B1) until Nov 15",
            "🎬 New screening at CGV - Spider-Man: Beyond the Multiverse"
        };

        // Sample header menu (you can load this from DB later)
        private static readonly List<(string Name, string Url)> MenuItems = new()
        {
            ("Stores", "/Mall/Stores"),
            ("Dining", "/Mall/Dining"),
            ("Entertainment", "/Mall/Entertainment"),
            ("Events", "/Mall/Events"),
            ("Promotions", "/Mall/Promotions")
        };

        public IActionResult Index()
        {
            ViewData["Title"] = "Home";
            ViewData["MallName"] = "ABCD Mall";
            ViewBag.Notifications = Notifications;
            ViewBag.MenuItems = MenuItems;
            return View();
        }

        // Action for header partial
        public IActionResult Header()
        {
            ViewBag.MallName = "ABCD Mall";
            ViewBag.MenuItems = MenuItems;
            ViewBag.Notifications = Notifications;
            return PartialView("_Header");
        }

        // Test action: renders the full layout for quick checks
        public IActionResult TestLayout()
        {
            ViewData["Title"] = "Layout Test";
            ViewData["MallName"] = "ABCD Mall";
            ViewBag.Notifications = Notifications;
            ViewBag.MenuItems = MenuItems;
            return View();
        }
    }
}
