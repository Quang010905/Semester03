using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Semester03.Areas.Client.Models.ViewModels;
using Semester03.Areas.Client.Repositories;

namespace Semester03.Areas.Client.Controllers
{
    [Area("Client")]
    public class EventController : Controller
    {
        // NOTE: using singleton repository instance directly.
        private readonly EventRepository _repo = EventRepository.Instance;

        public EventController()
        {
        }

        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Events";
            ViewData["MallName"] = ViewData["MallName"] ?? "ABCD Mall";

            var vm = new EventHomeVm
            {
                Featured = await _repo.GetFeaturedEventsAsync(3),
                Upcoming = await _repo.GetUpcomingEventsAsync()
            };

            // expose for layout partial
            ViewBag.Events = vm.Upcoming;

            return View(vm);
        }

        public async Task<IActionResult> Details(int id)
        {
            var ev = await _repo.GetEventByIdAsync(id);
            if (ev == null) return NotFound();

            ViewData["Title"] = ev.Title;

            // also expose upcoming to layout partial (so sidebar works on details page)
            ViewBag.Events = await _repo.GetUpcomingEventsAsync();

            return View(ev);
        }

        [HttpGet]
        public async Task<IActionResult> ListPartial(int top = 5)
        {
            var events = await _repo.GetUpcomingEventsAsync();
            return PartialView("_EventListPartial", events.Take(top));
        }
    }
}
