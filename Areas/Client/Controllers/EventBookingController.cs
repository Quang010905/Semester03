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
using Microsoft.AspNetCore.Authorization;

namespace Semester03.Areas.Client.Controllers
{
    [Area("Client")]
    public class EventBookingController : ClientBaseController
    {
        private readonly AbcdmallContext _context;
        private readonly EventRepository _eventRepo;
        private readonly EventBookingRepository _bookingRepo;
        private readonly IVnPayService _vnPayService;
        private readonly TicketEmailService _ticketEmailService;
        private readonly ILogger<EventBookingController> _logger;

        public EventBookingController(
            TenantTypeRepository tenantTypeRepo,
            AbcdmallContext context,
            EventRepository eventRepo,
            EventBookingRepository bookingRepo,
            IVnPayService vnPayService,
            TicketEmailService ticketEmailService,
            ILogger<EventBookingController> logger
        ) : base(tenantTypeRepo)
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

        // =====================================================================================
        // GET: Client/EventBooking/Register/5
        // =====================================================================================
        [HttpGet]
        public async Task<IActionResult> Register(int id)
        {
            var evt = await _eventRepo.GetEventByIdAsync(id);
            if (evt == null) return NotFound();

            var confirmed = await _bookingRepo.GetConfirmedSlotsForEventAsync(id);
            var available = Math.Max(0, evt.MaxSlot - confirmed);

            decimal pricePerTicket = evt.Price ?? 0m;

            if (pricePerTicket <= 0m)
            {
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
                    _logger.LogWarning(ex, "Register(): fallback price error");
                }
            }

            ViewBag.Price = pricePerTicket;

            var vm = new EventRegisterVm
            {
                Event = evt,
                AvailableSlots = available
            };

            return View("Register", vm);
        }

        // =====================================================================================
        // POST: Client/EventBooking/CreateBooking
        // =====================================================================================
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
                    if (IsAjaxRequest()) return Json(new { success = false, message = "Sự kiện không tồn tại." });
                    TempData["BookingError"] = "Sự kiện không tồn tại.";
                    return RedirectToAction("Index", "Event");
                }

                var confirmed = await _bookingRepo.GetConfirmedSlotsForEventAsync(eventId);
                var available = Math.Max(0, evt.MaxSlot - confirmed);

                if (quantity > available)
                {
                    var msg = $"Không đủ slot. Còn {available} slot.";

                    if (IsAjaxRequest()) return Json(new { success = false, message = msg });
                    TempData["BookingError"] = msg;
                    return RedirectToAction("Register", new { id = eventId });
                }

                // ===== LẤY GIÁ =====
                decimal pricePerTicket = evt.Price ?? 0m;

                if (pricePerTicket <= 0m)
                {
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
                        _logger.LogWarning(ex, "CreateBooking(): fallback price error");
                    }
                }

                var totalCost = pricePerTicket * quantity;

                int? userId = null;
                var claim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(claim, out var uid)) userId = uid;

                var notes = $"ContactName:{contactName};ContactEmail:{contactEmail};Qty:{quantity}";

                // =====================================================================================
                // FREE EVENT
                // =====================================================================================
                if (totalCost <= 0m)
                {
                    var booking = await _bookingRepo.CreateBookingAsync(
                        tenantId: evt.TenantPositionId,
                        userId: userId,
                        eventId: eventId,
                        totalCost: totalCost,
                        quantity: quantity,
                        notes: notes
                    );

                    // Gửi email xác nhận đặt vé sự kiện (chỉ khi có userId)
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            if (booking.EventBookingUserId != 0)
                            {
                                await _ticketEmailService.SendEventBookingSuccessEmailAsync(
                                    userId: booking.EventBookingUserId,
                                    bookingId: booking.EventBookingId,
                                    eventName: evt.Title,
                                    eventStart: evt.StartDate,
                                    eventEnd: evt.EndDate,
                                    location: evt.PositionLocation,
                                    organizer: evt.OrganizerShopName,
                                    quantity: booking.EventBookingQuantity ?? quantity,
                                    unitPrice: pricePerTicket,
                                    totalAmount: booking.EventBookingTotalCost ?? totalCost,
                                    purchaseDate: DateTime.Now
                                );
                            }
                        }
                        catch { }
                    });

                    if (IsAjaxRequest())
                    {
                        return Json(new
                        {
                            success = true,
                            redirectUrl = Url.Action("BookingSuccess", new { id = booking.EventBookingId })
                        });
                    }

                    return RedirectToAction("BookingSuccess", new { id = booking.EventBookingId });
                }

                // =====================================================================================
                // PAID EVENT
                // =====================================================================================
                var pendingBooking = await _bookingRepo.CreateBookingAsync(
                    tenantId: evt.TenantPositionId,
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
                        $"Event:{eventId};BookingId:{pendingBooking.EventBookingId};Qty:{quantity};Amount:{totalCost}",
                    Name = $"Event Booking #{pendingBooking.EventBookingId} - {evt.Title}"
                };

                var paymentUrl = _vnPayService.CreatePaymentUrl(payModel, HttpContext);

                if (IsAjaxRequest())
                {
                    return Json(new { success = true, url = paymentUrl });
                }

                return Redirect(paymentUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CreateBooking error");

                if (IsAjaxRequest()) return Json(new { success = false, message = "Lỗi server." });

                TempData["BookingError"] = "Lỗi hệ thống khi đặt vé.";
                return RedirectToAction("Register", new { id = eventId });
            }
        }

        // =====================================================================================
        // VNPAY CALLBACK
        // =====================================================================================
        [HttpGet("/Client/EventBooking/PaymentCallbackVnpay")]
        public async Task<IActionResult> PaymentCallbackVnpay()
        {
            try
            {
                var response = _vnPayService.PaymentExecute(Request.Query);
                if (response == null)
                    return RedirectToAction("PaymentFailed");

                int bookingId = 0;

                var parts = (response.OrderDescription ?? "")
                    .Split(';', StringSplitOptions.RemoveEmptyEntries);

                foreach (var p in parts)
                {
                    if (p.StartsWith("BookingId:"))
                        int.TryParse(p.Replace("BookingId:", "").Trim(), out bookingId);
                }

                if (!response.Success)
                    return RedirectToAction("PaymentFailed");

                var marked = await _bookingRepo.MarkBookingPaidAsync(bookingId);
                if (!marked)
                {
                    _logger.LogWarning("PaymentCallbackVnpay: MarkBookingPaidAsync({BookingId}) failed", bookingId);
                    return RedirectToAction("PaymentFailed", new { message = "Không tìm thấy booking hoặc không cập nhật được trạng thái thanh toán." });
                }

                // ========== GỬI EMAIL XÁC NHẬN ĐẶT VÉ SỰ KIỆN SAU KHI THANH TOÁN THÀNH CÔNG ==========
                try
                {
                    var booking = await _bookingRepo.GetByIdAsync(bookingId);
                    if (booking != null && booking.EventBookingUserId != 0)
                    {
                        var evt = await _eventRepo.GetEventByIdAsync(booking.EventBookingEventId);

                        if (evt != null)
                        {
                            await _ticketEmailService.SendEventBookingSuccessEmailAsync(
                                userId: booking.EventBookingUserId,
                                bookingId: booking.EventBookingId,
                                eventName: evt.Title,
                                eventStart: evt.StartDate,
                                eventEnd: evt.EndDate,
                                location: evt.PositionLocation,
                                organizer: evt.OrganizerShopName,
                                quantity: booking.EventBookingQuantity ?? 1,
                                unitPrice: booking.EventBookingUnitPrice ?? (evt.Price ?? 0m),
                                totalAmount: booking.EventBookingTotalCost ?? 0m,
                                purchaseDate: DateTime.Now
                            );
                        }
                    }
                }
                catch (Exception exMail)
                {
                    _logger.LogError(exMail, "Error sending event booking email after VNPAY callback for booking {BookingId}", bookingId);
                }

                return RedirectToAction("BookingSuccess", new { id = bookingId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PaymentCallbackVnpay error");
                return RedirectToAction("PaymentFailed");
            }
        }

        // =====================================================================================
        // GET: Client/EventBooking/Details/{id}
        // =====================================================================================
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Details(int id)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr))
            {
                return RedirectToAction(
                    "Login",
                    "Account",
                    new { area = "Client", returnUrl = Url.Action("Details", "EventBooking", new { area = "Client", id }) }
                );
            }

            int userId = int.Parse(userIdStr);

            var booking = await _bookingRepo.GetByIdAsync(id);
            if (booking == null || booking.EventBookingUserId != userId)
            {
                return NotFound();
            }

            var evtDetail = await _eventRepo.GetEventByIdAsync(booking.EventBookingEventId, userId);
            if (evtDetail == null)
            {
                return NotFound();
            }

            var now = DateTime.Now;

            string status =
                (booking.EventBookingStatus ?? 0) == 0 ? "Đã hủy" :
                (evtDetail.EndDate ?? evtDetail.StartDate) <= now ? "Đã diễn ra" :
                "Sắp diễn ra";

            string qrUrl = Url.Action(
                "Details",
                "EventBooking",
                new { area = "Client", id = booking.EventBookingId },
                protocol: HttpContext.Request.Scheme
            );

            string qrImg = "https://api.qrserver.com/v1/create-qr-code/?size=200x200&data=" +
                           System.Net.WebUtility.UrlEncode(qrUrl);

            var vm = new EventTicketDetailVm
            {
                BookingId = booking.EventBookingId,
                EventName = evtDetail.Title,
                EventImg = string.IsNullOrEmpty(evtDetail.ImageUrl)
                    ? "/images/event-placeholder.png"
                    : evtDetail.ImageUrl,
                Description = evtDetail.Description,
                EventStart = evtDetail.StartDate,
                EventEnd = (evtDetail.EndDate ?? evtDetail.StartDate),
                Quantity = booking.EventBookingQuantity ?? 1,
                TotalCost = booking.EventBookingTotalCost ?? 0m,
                UnitPrice = booking.EventBookingUnitPrice ?? (evtDetail.Price ?? 0m),
                Status = status,
                PositionLocation = evtDetail.PositionLocation ?? "",
                PositionFloor = evtDetail.PositionFloor,
                OrganizerShopName = evtDetail.OrganizerShopName ?? "",
                QRCodeUrl = qrImg
            };

            return View(vm);
        }

        // =====================================================================================
        // SUCCESS PAGE
        // =====================================================================================
        [HttpGet]
        public async Task<IActionResult> BookingSuccess(int id)
        {
            var booking = await _bookingRepo.GetByIdAsync(id);
            if (booking == null) return RedirectToAction("Index", "Event");

            var evt = await _eventRepo.GetEventByIdAsync(booking.EventBookingEventId);

            var vm = new EventBookingSuccessVm
            {
                BookingId = booking.EventBookingId,
                EventTitle = evt?.Title ?? "Event",
                Quantity = ExtractQtyFromNotes(booking.EventBookingNotes),
                Amount = booking.EventBookingTotalCost ?? 0,
                ContactEmail = ExtractContactEmailFromNotes(booking.EventBookingNotes),
                PaymentStatus = booking.EventBookingPaymentStatus ?? 0
            };

            return View("BookingSuccess", vm);
        }

        [HttpGet]
        public IActionResult PaymentFailed(string message = "")
        {
            ViewData["Message"] = message;
            return View("PaymentFailed");
        }

        // =====================================================================================
        // HELPERS
        // =====================================================================================
        private bool IsAjaxRequest()
        {
            try
            {
                var xreq = Request.Headers["X-Requested-With"].FirstOrDefault();
                if (xreq == "XMLHttpRequest") return true;

                var accept = Request.Headers["Accept"].FirstOrDefault();
                if (accept != null && accept.Contains("application/json")) return true;

                if (Request.Query.ContainsKey("ajax")) return true;
            }
            catch { }
            return false;
        }

        private int ExtractQtyFromNotes(string notes)
        {
            if (string.IsNullOrWhiteSpace(notes)) return 1;
            try
            {
                var part = notes.Split(';')
                    .FirstOrDefault(p => p.StartsWith("Qty", StringComparison.OrdinalIgnoreCase));
                if (part == null) return 1;

                var val = new string(part.Where(char.IsDigit).ToArray());
                if (int.TryParse(val, out var q)) return q;
            }
            catch { }
            return 1;
        }

        private string ExtractContactEmailFromNotes(string notes)
        {
            if (string.IsNullOrWhiteSpace(notes)) return "";
            try
            {
                var part = notes.Split(';')
                    .FirstOrDefault(p => p.StartsWith("ContactEmail:", StringComparison.OrdinalIgnoreCase));

                if (part == null) return "";
                return part.Replace("ContactEmail:", "").Trim();
            }
            catch { }
            return "";
        }

        // =====================================================================================
        // PARTIAL CANCEL EVENT TICKETS
        // =====================================================================================
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> PartialCancel(int bookingId, int cancelQuantity)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var booking = await _bookingRepo.GetByIdAsync(bookingId);

            if (booking == null || booking.EventBookingUserId != userId)
                return Json(new { success = false, message = "Không tìm thấy đơn đặt vé." });

            int qty = booking.EventBookingQuantity ?? 0;

            if (cancelQuantity <= 0)
                return Json(new { success = false, message = "Số lượng hủy phải > 0." });

            if (cancelQuantity > qty)
                return Json(new { success = false, message = "Số vé cần hủy vượt quá số vé còn lại." });

            int remainingQty = qty - cancelQuantity;
            decimal unitPrice = booking.EventBookingUnitPrice ?? 0;
            decimal refundAmount = unitPrice * cancelQuantity;

            booking.EventBookingQuantity = remainingQty;
            booking.EventBookingTotalCost = remainingQty * unitPrice;

            booking.EventBookingPaymentStatus = remainingQty > 0 ? 2 : 3;
            booking.EventBookingStatus = remainingQty > 0 ? 1 : 0;

            string smallLog =
                $"[Cancel {DateTime.Now:dd/MM HH:mm}] Hủy {cancelQuantity} vé (còn {remainingQty}).";

            booking.EventBookingNotes =
                string.IsNullOrWhiteSpace(booking.EventBookingNotes)
                    ? smallLog
                    : booking.EventBookingNotes + "\n" + smallLog;

            var history = new TblEventBookingHistory
            {
                EventBookingHistoryBookingId = booking.EventBookingId,
                EventBookingHistoryEventId = booking.EventBookingEventId,
                EventBookingHistoryUserId = userId,
                EventBookingHistoryAction = "PartialCancel",
                EventBookingHistoryQuantity = cancelQuantity,
                EventBookingHistoryDetails =
                    $"User cancelled {cancelQuantity} ticket(s). From {qty} → {remainingQty}. Refund {refundAmount:N0}",
                EventBookingHistoryRelatedDate = DateOnly.FromDateTime(DateTime.Now),
                EventBookingHistoryCreatedAt = DateTime.Now
            };

            _context.TblEventBookingHistories.Add(history);
            _context.Update(booking);
            await _context.SaveChangesAsync();

            try
            {
                var evt = await _eventRepo.GetEventByIdAsync(booking.EventBookingEventId);
                string eventName = evt?.Title ?? "Sự kiện";

                await _ticketEmailService.SendEventCancelEmailAsync(
                    userId: booking.EventBookingUserId,
                    eventName: eventName,
                    bookingId: booking.EventBookingId,
                    cancelledQty: cancelQuantity,
                    remainingQty: remainingQty,
                    refundAmount: refundAmount
                );
            }
            catch
            {
            }

            return Json(new
            {
                success = true,
                message = $"Đã hủy {cancelQuantity} vé, hoàn lại {refundAmount:N0}đ."
            });
        }
    }
}
