using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Semester03.Areas.Client.Models.ViewModels;
using Semester03.Models.Entities;
using Semester03.Models.Repositories;
using Semester03.Services.Vnpay;
using Semester03.Services.Email;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Semester03.Areas.Client.Controllers
{
    [Area("Client")]
    public class EventBookingController : Controller
    {
        private readonly AbcdmallContext _context;
        private readonly EventRepository _eventRepo;
        private readonly EventBookingRepository _bookingRepo;
        private readonly IVnPayService _vnPayService;
        private readonly TicketEmailService _ticketEmailService;
        private readonly ILogger<EventBookingController> _logger;

        public EventBookingController(
            AbcdmallContext context,
            EventRepository eventRepo,
            EventBookingRepository bookingRepo,
            IVnPayService vnPayService,
            TicketEmailService ticketEmailService,
            ILogger<EventBookingController> logger)
        {
            _context = context;
            _eventRepo = eventRepo;
            _bookingRepo = bookingRepo;
            _vnPayService = vnPayService;
            _ticketEmailService = ticketEmailService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Ping() => Content("Ping OK");

        // GET: Client/EventBooking/Register/5
        [HttpGet]
        public async Task<IActionResult> Register(int id)
        {
            var evt = await _eventRepo.GetEventByIdAsync(id);
            if (evt == null) return NotFound();

            var confirmed = await _bookingRepo.GetConfirmedSlotsForEventAsync(id);
            var maxSlot = evt.MaxSlot;
            var available = Math.Max(0, maxSlot - confirmed);

            decimal pricePerTicket = 0m;
            try
            {
                var evtEntity = await _context.TblEvents.FindAsync(id);
                if (evtEntity != null)
                {
                    var prop = evtEntity.GetType().GetProperty("EventPrice")
                               ?? evtEntity.GetType().GetProperty("Price")
                               ?? evtEntity.GetType().GetProperty("TicketPrice");
                    if (prop != null)
                    {
                        var raw = prop.GetValue(evtEntity);
                        if (raw != null) pricePerTicket = Convert.ToDecimal(raw);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cannot read price for event; default to 0.");
                pricePerTicket = 0m;
            }

            ViewBag.Price = pricePerTicket;

            var vm = new EventRegisterVm
            {
                Event = new EventDetailsVm
                {
                    Id = evt.Id,
                    Title = evt.Title,
                    Description = evt.Description,
                    StartDate = evt.StartDate,
                    EndDate = evt.EndDate,
                    ImageUrl = evt.ImageUrl,
                    MaxSlot = evt.MaxSlot,
                    Status = evt.Status,
                    TenantPositionId = evt.TenantPositionId,
                    TenantName = evt.TenantName
                },
                AvailableSlots = available
            };

            return View("Register", vm);
        }

        // POST: Client/EventBooking/CreateBooking (AJAX hoặc form thường)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBooking(
            [FromForm] int eventId,
            [FromForm] string contactName,
            [FromForm] string contactEmail,
            [FromForm] int quantity = 1)
        {
            try
            {
                if (quantity <= 0) quantity = 1;

                var evt = await _eventRepo.GetEventByIdAsync(eventId);
                if (evt == null)
                {
                    _logger.LogWarning("CreateBooking: event {EventId} not found", eventId);
                    if (IsAjaxRequest()) return Json(new { success = false, message = "Sự kiện không tồn tại." });
                    TempData["BookingError"] = "Sự kiện không tồn tại.";
                    return RedirectToAction("Index", "Event", new { area = "Client" });
                }

                // Tính slot đã confirm
                var confirmed = await _bookingRepo.GetConfirmedSlotsForEventAsync(eventId);
                var maxSlot = evt.MaxSlot;
                var available = Math.Max(0, maxSlot - confirmed);
                if (quantity > available)
                {
                    var msg = $"Không đủ slot. Còn {available} slot.";
                    _logger.LogInformation(
                        "CreateBooking: not enough slots for event {EventId}. Requested {Req}, Available {Avail}",
                        eventId, quantity, available);

                    if (IsAjaxRequest()) return Json(new { success = false, message = msg });
                    TempData["BookingError"] = msg;
                    return RedirectToAction("Register", new { area = "Client", id = eventId });
                }

                // Lấy giá
                decimal pricePerTicket = 0m;
                try
                {
                    var evtEntity = await _context.TblEvents.FindAsync(eventId);
                    if (evtEntity != null)
                    {
                        var prop = evtEntity.GetType().GetProperty("EventPrice")
                                   ?? evtEntity.GetType().GetProperty("Price")
                                   ?? evtEntity.GetType().GetProperty("TicketPrice");
                        if (prop != null)
                        {
                            var raw = prop.GetValue(evtEntity);
                            if (raw != null) pricePerTicket = Convert.ToDecimal(raw);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error reading price for event {EventId}", eventId);
                    pricePerTicket = 0m;
                }

                var totalCost = pricePerTicket * quantity;
                _logger.LogDebug("CreateBooking: event {EventId}, qty {Qty}, price {Price}, total {Total}",
                    eventId, quantity, pricePerTicket, totalCost);

                int? userId = null;
                var claim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrWhiteSpace(claim) && int.TryParse(claim, out var parsedUser))
                    userId = parsedUser;

                // Lưu tenantId = TenantPositionId như code gốc (FK hơi lệch nhưng tớ không đổi để tránh vỡ)
                var tenantId = evt.TenantPositionId;
                var notes = $"ContactName:{contactName};ContactEmail:{contactEmail};Qty:{quantity}";

                if (totalCost <= 0m)
                {
                    // FREE EVENT – tạo booking và xác nhận luôn
                    var booking = await _bookingRepo.CreateBookingAsync(
                        tenantId: tenantId,
                        userId: userId,
                        eventId: eventId,
                        totalCost: totalCost,
                        quantity: quantity,
                        notes: notes
                    );

                    // Gửi email (fire & forget)
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            if (userId.HasValue)
                                await _ticketEmailService.SendTicketsEmailAsync(userId.Value, new System.Collections.Generic.List<int>());
                        }
                        catch (Exception ex) { _logger.LogError(ex, "Error sending booking email (free event)"); }
                    });

                    if (IsAjaxRequest())
                    {
                        var redirectUrl = Url.Action("BookingSuccess", "EventBooking",
                            new { area = "Client", id = booking.EventBookingId });
                        return Json(new { success = true, redirectUrl });
                    }

                    return RedirectToAction("BookingSuccess", "EventBooking",
                        new { area = "Client", id = booking.EventBookingId });
                }
                else
                {
                    // Event có phí – tạo booking Pending, rồi redirect sang VNPAY
                    var bookingPending = await _bookingRepo.CreateBookingAsync(
                        tenantId: tenantId,
                        userId: userId,
                        eventId: eventId,
                        totalCost: totalCost,
                        quantity: quantity,
                        notes: notes
                    );

                    var payModel = new Semester03.Areas.Client.Models.Vnpay.PaymentInformationModel
                    {
                        OrderType = "event-ticket",
                        Amount = (double)totalCost,
                        OrderDescription =
                            $"Event:{eventId};BookingId:{bookingPending.EventBookingId};Qty:{quantity};Amount:{totalCost}",
                        Name = $"Event Booking #{bookingPending.EventBookingId} - {evt.Title}"
                    };

                    var url = _vnPayService.CreatePaymentUrl(payModel, HttpContext);

                    if (string.IsNullOrWhiteSpace(url))
                    {
                        _logger.LogError("VnPay service returned null or empty URL for booking {BookingId}",
                            bookingPending.EventBookingId);
                        if (IsAjaxRequest()) return Json(new { success = false, message = "Không thể tạo link thanh toán." });
                        TempData["BookingError"] = "Không thể tạo link thanh toán. Vui lòng thử lại sau.";
                        return RedirectToAction("Register", new { area = "Client", id = eventId });
                    }

                    if (IsAjaxRequest()) return Json(new { success = true, url });
                    return Redirect(url);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CreateBooking error");
                if (IsAjaxRequest()) return Json(new { success = false, message = "Lỗi server khi tạo đặt chỗ." });

                TempData["BookingError"] = "Có lỗi xảy ra khi đặt chỗ. Vui lòng thử lại sau.";
                return RedirectToAction("Register", new { area = "Client", id = eventId });
            }
        }

        // VNPAY callback (GET) cho EVENT
        [HttpGet]
        public async Task<IActionResult> PaymentCallbackVnpay()
        {
            try
            {
                var response = _vnPayService.PaymentExecute(Request.Query);
                if (response == null)
                    return RedirectToAction("PaymentFailed",
                        new { area = "Client", message = "Không nhận được phản hồi từ VNPAY" });

                int bookingId = 0;
                var parts = (response.OrderDescription ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries);
                foreach (var p in parts)
                {
                    var t = p.Trim();
                    if (t.StartsWith("BookingId:", StringComparison.OrdinalIgnoreCase))
                        int.TryParse(t.Substring("BookingId:".Length).Trim(), out bookingId);
                }

                if (!response.Success)
                {
                    // LOG HISTORY PAYMENT FAILED
                    if (bookingId > 0)
                    {
                        var booking = await _bookingRepo.GetByIdAsync(bookingId);
                        if (booking != null)
                        {
                            await _bookingRepo.AddHistoryAsync(
                                booking.EventBookingId,
                                booking.EventBookingEventId,
                                booking.EventBookingUserId,
                                "PaymentFailed",
                                $"Payment failed. VNPAY Code: {response.VnPayResponseCode}",
                                DateTime.UtcNow.Date,
                                null
                            );
                        }
                    }

                    return RedirectToAction("PaymentFailed",
                        new { area = "Client", message = $"Thanh toán thất bại (code: {response.VnPayResponseCode})" });
                }

                if (bookingId <= 0)
                {
                    _logger.LogWarning("VNPAY callback: BookingId not found in OrderDescription");
                    return RedirectToAction("PaymentFailed",
                        new { area = "Client", message = "Không xác định booking." });
                }

                var marked = await _bookingRepo.MarkBookingPaidAsync(bookingId);
                if (!marked) _logger.LogWarning("Failed to mark booking {BookingId} as paid", bookingId);

                try
                {
                    var booking = await _bookingRepo.GetByIdAsync(bookingId);
                    if (booking != null && booking.EventBookingUserId > 0)
                    {
                        // gửi email
                        await _ticketEmailService.SendTicketsEmailAsync(
                            booking.EventBookingUserId,
                            new System.Collections.Generic.List<int>()
                        );
                    }
                }
                catch (Exception exEmail)
                {
                    _logger.LogError(exEmail, "Error sending booking confirmation email after payment.");
                }

                return RedirectToAction("BookingSuccess", "EventBooking",
                    new { area = "Client", id = bookingId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PaymentCallbackVnpay error");
                return RedirectToAction("PaymentFailed",
                    new { area = "Client", message = "Lỗi xử lý VNPAY." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> BookingSuccess(int id)
        {
            var booking = await _bookingRepo.GetByIdAsync(id);
            if (booking == null)
            {
                TempData["BookingError"] = "Không tìm thấy đơn đặt vé.";
                return RedirectToAction("Index", "Event", new { area = "Client" });
            }

            var evt = await _eventRepo.GetEventByIdAsync(booking.EventBookingEventId);
            decimal amount = 0m;
            try { amount = Convert.ToDecimal(booking.EventBookingTotalCost); } catch { amount = 0m; }

            var vm = new EventBookingSuccessVm
            {
                BookingId = booking.EventBookingId,
                EventTitle = evt?.Title ?? $"Event #{booking.EventBookingEventId}",
                Quantity = ExtractQtyFromNotes(booking.EventBookingNotes),
                Amount = amount,
                ContactEmail = ExtractContactEmailFromNotes(booking.EventBookingNotes),
                PaymentStatus = booking.EventBookingPaymentStatus ?? 0
            };

            return View("BookingSuccess", vm);
        }

        [HttpGet]
        public IActionResult PaymentFailed(string message = "")
        {
            ViewData["Message"] = string.IsNullOrEmpty(message)
                ? "Thanh toán thất bại hoặc bị hủy."
                : message;
            return View("PaymentFailed");
        }

        // ==========================
        // Helpers
        // ==========================

        // MỞ RỘNG kiểm tra AJAX: X-Requested-With hoặc Accept: application/json hoặc ?ajax=1
        private bool IsAjaxRequest()
        {
            try
            {
                var xreq = Request.Headers["X-Requested-With"].FirstOrDefault();
                if (!string.IsNullOrEmpty(xreq) &&
                    xreq.Equals("XMLHttpRequest", StringComparison.OrdinalIgnoreCase))
                    return true;

                var accept = Request.Headers["Accept"].FirstOrDefault();
                if (!string.IsNullOrEmpty(accept) &&
                    accept.IndexOf("application/json", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;

                if (Request.Query.ContainsKey("ajax")) return true;

                return false;
            }
            catch
            {
                return false;
            }
        }

        private int ExtractQtyFromNotes(string notes)
        {
            if (string.IsNullOrWhiteSpace(notes)) return 1;
            try
            {
                var qpart = notes.Split(';')
                    .FirstOrDefault(p => p.Trim().StartsWith("Qty", StringComparison.OrdinalIgnoreCase));
                if (qpart == null) return 1;
                var idx = qpart.IndexOfAny(new[] { ':', '=', ' ' });
                string val = idx >= 0 ? qpart.Substring(idx + 1).Trim() : qpart.Substring(3).Trim();
                if (int.TryParse(new string(val.Where(char.IsDigit).ToArray()), out var q))
                    return Math.Max(1, q);
            }
            catch { }
            return 1;
        }

        private string ExtractContactEmailFromNotes(string notes)
        {
            if (string.IsNullOrWhiteSpace(notes)) return "";
            try
            {
                var ep = notes.Split(';')
                    .FirstOrDefault(p => p.Trim().StartsWith("ContactEmail:", StringComparison.OrdinalIgnoreCase));
                if (ep == null) return "";
                var v = ep.Substring(ep.IndexOf(':') + 1).Trim();
                return v;
            }
            catch { return ""; }
        }
    }
}
