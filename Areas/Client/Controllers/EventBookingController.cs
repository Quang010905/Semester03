using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Semester03.Areas.Client.Models.ViewModels;
using Semester03.Models.Entities;
using Semester03.Models.Repositories;
using Semester03.Services.Vnpay;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Semester03.Areas.Client.Controllers
{
    [Area("Client")]
    [Route("Client/[controller]/[action]")]
    public class EventBookingController : Controller
    {
        private readonly EventRepository _eventRepo;
        private readonly EventBookingRepository _bookingRepo;
        private readonly AbcdmallContext _context;
        private readonly IVnPayService _vnPayService;
        private readonly ILogger<EventBookingController> _logger;

        public EventBookingController(
            EventRepository eventRepo,
            EventBookingRepository bookingRepo,
            AbcdmallContext context,
            IVnPayService vnPayService,
            ILogger<EventBookingController> logger)
        {
            _eventRepo = eventRepo ?? throw new ArgumentNullException(nameof(eventRepo));
            _bookingRepo = bookingRepo ?? throw new ArgumentNullException(nameof(bookingRepo));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _vnPayService = vnPayService; // may be null if payment not used
            _logger = logger;
        }

        /// <summary>
        /// Show register page for event (uses EventDetailsVm and EventRegisterVm from shared viewmodels).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Register(int id)
        {
            var evt = await _eventRepo.GetEventByIdAsync(id);
            if (evt == null) return NotFound();

            // compute confirmed slots (responses with payment status 1 or 2)
            var confirmed = await _bookingRepo.GetConfirmedSlotsForEventAsync(id);
            var maxSlot = evt.MaxSlot;
            var available = Math.Max(0, maxSlot - confirmed);

            var vm = new EventRegisterVm
            {
                Event = evt,
                AvailableSlots = available
            };

            return View(vm);
        }

        /// <summary>
        /// Create booking. Quantity stored inside EventBooking_Notes as "Qty:N;ContactName:...;ContactEmail:..."
        /// totalAmount = total cost for the whole booking (pricePerPerson * qty).
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBooking([FromForm] int eventId,
            [FromForm] int quantity = 1,
            [FromForm] string contactName = "",
            [FromForm] string contactEmail = "",
            [FromForm] decimal totalAmount = 0m)
        {
            try
            {
                if (quantity <= 0) quantity = 1;

                var evt = await _eventRepo.GetEventByIdAsync(eventId);
                if (evt == null) return NotFound();

                // availability check
                var confirmed = await _bookingRepo.GetConfirmedSlotsForEventAsync(eventId);
                var available = Math.Max(0, evt.MaxSlot - confirmed);
                if (quantity > available)
                    return BadRequest($"Chỉ còn {available} chỗ trống.");

                // buyer id
                int? buyerId = null;
                if (User?.Identity?.IsAuthenticated == true)
                {
                    var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    if (int.TryParse(claim, out var uid)) buyerId = uid;
                }

                // resolve tenant id from Event -> TenantPosition -> AssignedTenantID
                int tenantId = 0;
                try
                {
                    // get event entity (admin repo method returns Tbl_Event entity)
                    var eventEntity = await _context.TblEvents
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x => x.EventId == eventId);

                    if (eventEntity != null && eventEntity.EventTenantPositionId > 0)
                    {
                        var pos = await _context.TblTenantPositions
                            .AsNoTracking()
                            .FirstOrDefaultAsync(p => p.TenantPositionId == eventEntity.EventTenantPositionId);

                        if (pos != null && pos.TenantPositionAssignedTenantId.HasValue)
                            tenantId = pos.TenantPositionAssignedTenantId.Value;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not resolve tenantId for event {EventId}", eventId);
                }

                // build notes string
                var notes = $"Qty:{quantity};ContactName:{contactName};ContactEmail:{contactEmail}";

                // create booking record (repo expects the SQL-shaped signature implemented earlier)
                var created = await _bookingRepo.CreateBookingAsync(
                    tenantId: tenantId,
                    userId: buyerId,
                    eventId: eventId,
                    totalCost: totalAmount,
                    quantity: quantity,
                    notes: notes
                );

                if (created == null)
                {
                    _logger.LogError("CreateBooking: creation returned null for event {EventId}", eventId);
                    return StatusCode(500, "Không thể tạo booking.");
                }

                // if payment required -> VNPAY
                if (totalAmount > 0 && _vnPayService != null)
                {
                    try
                    {
                        var model = new Semester03.Areas.Client.Models.Vnpay.PaymentInformationModel
                        {
                            OrderType = "event-booking",
                            Amount = (double)totalAmount,
                            OrderDescription = $"Event:{eventId};Booking:{created.EventBookingId};Qty:{quantity};Amount:{totalAmount}",
                            Name = $"Event booking #{created.EventBookingId}"
                        };

                        var url = _vnPayService.CreatePaymentUrl(model, HttpContext);
                        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                            return Json(new { success = true, url });

                        return Redirect(url);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "CreateBooking: error creating VNPAY url for booking {BookingId}", created.EventBookingId);
                        return StatusCode(500, "Lỗi tạo URL thanh toán.");
                    }
                }

                // free booking or payment not required -> done
                return RedirectToAction(nameof(BookingSuccess), new { bookingId = created.EventBookingId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CreateBooking: unhandled exception");
                return StatusCode(500, "Có lỗi xảy ra khi tạo booking.");
            }
        }

        [HttpGet]
        public async Task<IActionResult> PaymentCallbackVnpay()
        {
            if (_vnPayService == null)
            {
                return RedirectToAction(nameof(BookingFailed), new { message = "Cổng thanh toán chưa cấu hình." });
            }

            var resp = _vnPayService.PaymentExecute(Request.Query);
            if (resp == null)
                return RedirectToAction(nameof(BookingFailed), new { message = "Không nhận được phản hồi từ VNPAY" });

            int bookingId = 0;
            var parts = (resp.OrderDescription ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries);
            foreach (var p in parts)
            {
                var t = p.Trim();
                if (t.StartsWith("Booking:", StringComparison.OrdinalIgnoreCase))
                    int.TryParse(t.Substring("Booking:".Length).Trim(), out bookingId);
            }

            if (!resp.Success)
            {
                return RedirectToAction(nameof(BookingFailed), new { bookingId = bookingId, message = $"Thanh toán thất bại (Mã lỗi: {resp.VnPayResponseCode})" });
            }

            if (bookingId > 0)
            {
                var ok = await _bookingRepo.MarkBookingPaidAsync(bookingId);
                if (!ok)
                    _logger.LogWarning("PaymentCallbackVnpay: failed to mark booking {BookingId} as paid", bookingId);
            }

            return RedirectToAction(nameof(BookingSuccess), new { bookingId });
        }

        [HttpGet]
        public async Task<IActionResult> BookingSuccess(int bookingId)
        {
            var booking = await _bookingRepo.GetByIdAsync(bookingId);
            if (booking == null) return NotFound();

            // parse quantity and contact email from notes
            int qty = 1;
            string contactEmail = "";
            try
            {
                var notes = booking.EventBookingNotes ?? "";
                var parts = notes.Split(';', StringSplitOptions.RemoveEmptyEntries);
                foreach (var p in parts)
                {
                    var t = p.Trim();
                    if (t.StartsWith("Qty:", StringComparison.OrdinalIgnoreCase))
                        int.TryParse(t.Substring("Qty:".Length).Trim(), out qty);
                    else if (t.StartsWith("ContactEmail:", StringComparison.OrdinalIgnoreCase))
                        contactEmail = t.Substring("ContactEmail:".Length).Trim();
                }
            }
            catch { qty = 1; contactEmail = ""; }

            var vm = new EventBookingSuccessVm
            {
                BookingId = booking.EventBookingId,
                EventTitle = booking.EventBookingEvent?.EventName ?? "",
                Quantity = qty,
                Amount = booking.EventBookingTotalCost ?? 0m,
                ContactEmail = contactEmail
            };

            return View(vm);
        }

        [HttpGet]
        public IActionResult BookingFailed(int bookingId = 0, string message = "")
        {
            ViewData["Message"] = string.IsNullOrEmpty(message) ? "Thanh toán thất bại hoặc bị hủy." : message;
            ViewData["BookingId"] = bookingId;
            return View();
        }
    }
}
