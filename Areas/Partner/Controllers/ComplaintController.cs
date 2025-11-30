using Microsoft.AspNetCore.Mvc;
using Semester03.Areas.Partner.Models;
using Semester03.Models.Repositories;

namespace Semester03.Areas.Partner.Controllers
{
    [Area("Partner")]
    public class ComplaintController : Controller
    {
        private readonly ComplaintRepository _complaintRepo;
        private readonly EventRepository _eventRepository;
        // Inject repository qua constructor
        public ComplaintController(ComplaintRepository complaintRepo, EventRepository eventRepository)
        {
            _complaintRepo = complaintRepo;
            _eventRepository = eventRepository;
        }
        public async Task<IActionResult> Index(int id, int page = 1)
        {
            const int pageSize = 10;

            var list = await _complaintRepo.GetAllComplaintsByEventId(id);


            // Tổng số bình luận
            ViewBag.TotalCount = list.Count;
            ViewBag.Star5 = list.Count(c => c.Rate == 5);
            ViewBag.Star4 = list.Count(c => c.Rate == 4);
            ViewBag.Star3 = list.Count(c => c.Rate == 3);
            ViewBag.Star2 = list.Count(c => c.Rate == 2);
            ViewBag.Star1 = list.Count(c => c.Rate == 1);
            ViewBag.AvgRate = list.Count > 0 ? list.Average(c => c.Rate) : 0;

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

 
            ViewBag.listComplaint = data;
            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentPage = page;
            ViewBag.TotalItems = totalItems;
            ViewBag.PageSize = pageSize;
            ViewBag.StartIndex = (page - 1) * pageSize + 1;

            return View();
        }

        public async Task<ActionResult> Detail(int id)
        {
            var itemComplaint = await _complaintRepo.FindById(id);
            ViewBag.itemComplaint = itemComplaint;
            return View();
        }
    }
}
