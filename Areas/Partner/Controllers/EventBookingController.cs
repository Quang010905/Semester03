using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Semester03.Models.Repositories;

namespace Semester03.Areas.Partner.Controllers
{
    [Area("Partner")]
    [Authorize(Roles = "2")]
    public class EventBookingController : Controller
    {

        private readonly EventBookingRepository _eventBookingRepo;
        private readonly EventRepository _eventRepository;
        // Inject repository qua constructor
        public EventBookingController(EventBookingRepository eventBookingRepo, EventRepository eventRepository)
        {
            _eventBookingRepo = eventBookingRepo;
            _eventRepository = eventRepository;
        }
        public async Task<IActionResult> Index(int id, int page = 1)
        {
            const int pageSize = 10;

            var list = await _eventBookingRepo.GetAllBookingsByEventId(id);
             var totalItems = list.Count;
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            var data = list
                .OrderBy(c => c.Created)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Event info
            var itemEvent = await _eventRepository.FindById(id);
            ViewBag.itemEvent = itemEvent;


            ViewBag.listBooking = data;
            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentPage = page;
            ViewBag.TotalItems = totalItems;
            ViewBag.PageSize = pageSize;
            ViewBag.StartIndex = (page - 1) * pageSize + 1;

            return View();
        }

        public async Task<ActionResult> Detail(int id)
        {
            var itemEventBooking = await _eventBookingRepo.FindById(id);
            ViewBag.itemEventBooking = itemEventBooking;
            return View();
        }
    }
}
