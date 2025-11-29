using Microsoft.AspNetCore.Mvc;
using Semester03.Areas.Admin.Models;
using Semester03.Areas.Partner.Models;
using Semester03.Models.Repositories;
using System.Security.Claims;

namespace Semester03.Areas.Partner.Controllers
{
    [Area("Partner")]
    public class EventController : Controller
    {
        private readonly TenantPositionRepository _positionRepo;
        private readonly TenantRepository _tenantRepository;
        private readonly EventRepository _eventRepository;
        private readonly IWebHostEnvironment _webHostEnvironment;
        // Inject repository qua constructor
        public EventController(IWebHostEnvironment webHostEnvironment, TenantPositionRepository positionRepo, TenantRepository tenantRepository, EventRepository eventRepository)
        {
            _webHostEnvironment = webHostEnvironment;
            _positionRepo = positionRepo;
            _tenantRepository = tenantRepository;
            _eventRepository = eventRepository;
        }

        public async Task<IActionResult> Index(int id, int page = 1)
        {
            const int pageSize = 10;
            var list = await _positionRepo.GetAllPositionsByTenantId(id);

            var totalItems = list.Count;
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            var data = list
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            var itemTenant = await _tenantRepository.FindById(id);
            ViewBag.itemTenant = itemTenant;
            ViewBag.listPosition = data;
            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentPage = page;
            ViewBag.TotalItems = totalItems;
            ViewBag.PageSize = pageSize;
            ViewBag.StartIndex = (page - 1) * pageSize + 1;
            return View();
        }
        public async Task<IActionResult> CreateEvent(int id, int page = 1, string search = "")
        {
            const int pageSize = 10;
            var list = await _eventRepository.GetAllEventsByPositionId(id);
            string normalizedSearch = _eventRepository.NormalizeSearch(search);
            if (!string.IsNullOrWhiteSpace(normalizedSearch))
            {
                list = list
                    .Where(t => _eventRepository.NormalizeSearch(t.Name).Contains(normalizedSearch))
                    .ToList();
            }
            var totalItems = list.Count;
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            var data = list
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            var itemPosition = await _positionRepo.FindById(id);
            ViewBag.itemPosition = itemPosition;
            ViewBag.listEvent = data;
            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentPage = page;
            ViewBag.TotalItems = totalItems;
            ViewBag.PageSize = pageSize;
            ViewBag.StartIndex = (page - 1) * pageSize + 1;
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> AddEvent(IFormFile upFile)
        {
            string? eventName = Request.Form["EventName"];
            string? position = Request.Form["PositionId"];
            int positionId = int.Parse(position);
            string? pPrice = Request.Form["EventPrice"];
            int price = Convert.ToInt32(pPrice);
            string? mSlot = Request.Form["MaxSlot"];
            int maxSlot = Convert.ToInt32(mSlot);
            string? description = Request.Form["EventDescription"];
            string? startStr = Request.Form["Start"];
            string? endStr = Request.Form["End"];
            DateTime startDate = DateTime.Parse(startStr);
            DateTime endDate = DateTime.Parse(endStr);
            if (startDate.Date < DateTime.Today)
            {
                TempData["ErrorMessage"] = "Start date must be today or in the future!";
                return RedirectToAction("CreateEvent", "Event", new { id = positionId });
            }

            if (startDate >= endDate)
            {
                TempData["ErrorMessage"] = "Start date must be before end date!";
                return RedirectToAction("CreateEvent", "Event", new { id = positionId });
            }
            bool exists = await _eventRepository.CheckEventNameAsync(eventName, positionId);
            if (exists)
            {
                TempData["ErrorMessage"] = "Event name already exist";
                return RedirectToAction("CreateEvent", "Event", new { id = positionId });
            }
            string fileName = "";
            string pathSave = Path.Combine(_webHostEnvironment.WebRootPath, "Content/Uploads/Events");
            Directory.CreateDirectory(pathSave);
            try
            {
                if (upFile != null && upFile.Length > 0)
                {
                    fileName = $"{DateTime.Now.Ticks}_{Path.GetFileName(upFile.FileName)}";
                    string filePath = Path.Combine(pathSave, fileName);


                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        upFile.CopyTo(stream);
                    }
                }
                else
                {
                    fileName = "noimage.png";
                }
                if (string.IsNullOrWhiteSpace(description))
                {
                    TempData["ErrorMessage"] = "Please enter enough information!";
                    return RedirectToAction("CreateEvent", "Event", new { id = positionId });
                }

                var entity = new Event
                {
                    Name = eventName,
                    Img = fileName,
                    Status = 2,
                    Description = description,
                    Start = startDate,
                    End = endDate,
                    MaxSlot = maxSlot,
                    UnitPrice = price,
                    TenantPositionId = positionId,
                };

                await _eventRepository.AddEvent(entity);

                TempData["SuccessMessage"] = "Add event success!";
            }
            catch (Exception)
            {

                throw;
            }
            return RedirectToAction("CreateEvent", "Event", new { id = positionId });
        }

        public async Task<ActionResult> Edit(int id)
        {
            var itemEvent = await _eventRepository.FindById(id);
            ViewBag.itemEvent = itemEvent;
            return View();
        }
        [HttpGet]
        public async Task<ActionResult> DeleteEvent(int id, int positionId)
        {
            bool res = await _eventRepository.DeleteEvent(id);
            if (res)
            {
                TempData["SuccessMessage"] = "Delete event success";
            }
            else
            {
                TempData["ErrorMessage"] = "Delete fail";
            }
            return RedirectToAction("CreateEvent", "Event", new { id = positionId });
        }

        public async Task<ActionResult> UpdateEvent(IFormFile upFile)
        {
            string? eventName = Request.Form["EventName"];
            string? position = Request.Form["PositionId"];
            int positionId = int.Parse(position);
            string? pPrice = Request.Form["EventPrice"];
            decimal price = Convert.ToDecimal(pPrice);
            string? mSlot = Request.Form["MaxSlot"];
            int maxSlot = Convert.ToInt32(mSlot);
            string? description = Request.Form["EventDescription"];
            string? startStr = Request.Form["Start"];
            string? endStr = Request.Form["End"];
            string? eventId = Request.Form["EventId"];
            int eId = Convert.ToInt32(eventId);
            DateTime startDate = DateTime.Parse(startStr);
            DateTime endDate = DateTime.Parse(endStr);
            if (startDate.Date < DateTime.Today)
            {
                TempData["ErrorMessage"] = "Start date must be today or in the future!";
                return RedirectToAction("CreateEvent", "Event", new { id = positionId });
            }

            if (startDate >= endDate)
            {
                TempData["ErrorMessage"] = "Start date must be before end date!";
                return RedirectToAction("CreateEvent", "Event", new { id = positionId });
            }
            bool exists = await _eventRepository.CheckEventNameAsync(eventName, positionId, eId);
            if (exists)
            {
                TempData["ErrorMessage"] = "Event name already exist";
                return RedirectToAction("CreateEvent", "Event", new { id = positionId });
            }
            if (string.IsNullOrWhiteSpace(description))
            {
                TempData["ErrorMessage"] = "Please enter enough information!";
                return RedirectToAction("CreateEvent", "Event", new { id = positionId });
            }
            string fileName = "";
            string pathSave = Path.Combine(_webHostEnvironment.WebRootPath, "Content/Uploads/Events");

            Directory.CreateDirectory(pathSave);

            try
            {
                if (upFile != null && upFile.Length > 0)
                {
                    fileName = $"{DateTime.Now.Ticks}_{Path.GetFileName(upFile.FileName)}";
                    string filePath = Path.Combine(pathSave, fileName);


                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        upFile.CopyTo(stream);
                    }
                }
                else
                {
                    fileName = Request.Form["OldImage"];
                }

                var entity = new Event
                {
                    Id = eId,
                    Name = eventName,
                    Img = fileName,
                    Status = 2,
                    Description = description,
                    Start = startDate,
                    End = endDate,
                    MaxSlot = maxSlot,
                    UnitPrice = price,
                    TenantPositionId = positionId,
                };

                bool result = await _eventRepository.UpdateEvent(entity);
                TempData["SuccessMessage"] = "Update event success!";
            }
            catch (Exception ex)
            {
                var error = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                TempData["ErrorMessage"] = "Error: " + error;
            }

            return RedirectToAction("CreateEvent", "Event", new { id = positionId });
        }
    }
}
