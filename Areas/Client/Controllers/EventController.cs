using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Semester03.Areas.Client.Models.ViewModels;
using Semester03.Models.Repositories;

namespace Semester03.Areas.Client.Controllers
{
    [Area("Client")]
    public class EventController : Controller
    {
        private readonly EventRepository _repo;

        public EventController(EventRepository repo)
        {
            _repo = repo;
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

            // expose upcoming events cho layout (ví dụ phần sidebar)
            ViewBag.Events = vm.Upcoming;

            return View(vm);
        }

        public async Task<IActionResult> Details(int id)
        {
            var ev = await _repo.GetEventByIdAsync(id);
            if (ev == null)
                return NotFound();

            ViewData["Title"] = ev.Title;
            ViewBag.Events = await _repo.GetUpcomingEventsAsync();

            return View(ev);
        }

        [HttpGet]
        public async Task<IActionResult> ListPartial(int top = 5)
        {
            var events = await _repo.GetUpcomingEventsAsync(top);
            return PartialView("_EventListPartial", events);
        }
    }
}
