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
        // - BẮT BUỘC LOGIN
        // - Prefill tên + email của user hiện tại
        // =====================================================================================
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Register(int id)
        {
            var evt = await _eventRepo.GetEventByIdAsync(id);
            if (evt == null) return NotFound();

            // confirmed slots (Paid + Free) giống EventBookingRepository
            var confirmed = await _bookingRepo.GetConfirmedSlotsForEventAsync(id);
            var available = Math.Max(0, evt.MaxSlot - confirmed);

            // Giá vé
            decimal pricePerTicket = evt.Price ?? 0m;

            // Fallback nếu Price không map trong TblEvents
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

            // ===== LẤY USER HIỆN TẠI ĐỂ PREFILL TÊN + EMAIL =====
            try
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out var uid))
                {
                    var user = await _context.TblUsers.FindAsync(uid);
                    if (user != null)
                    {
                        ViewBag.DefaultContactName = string.IsNullOrWhiteSpace(user.UsersFullName)
                            ? user.UsersUsername
                            : user.UsersFullName;

                        ViewBag.DefaultContactEmail = user.UsersEmail;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Register(): error while pre-filling user info");
            }

            var vm = new EventRegisterVm
            {
                Event = evt,
                AvailableSlots = available
            };

            return View("Register", vm);
        }

        // =====================================================================================
        // POST: Client/EventBooking/CreateBooking
        // - BẮT BUỘC LOGIN
        // - FREE EVENT: mỗi user chỉ được 1 vé (quantity = 1)
        // =====================================================================================
        [HttpPost]
        [Authorize]
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

                // Lấy event từ repo
                var evt = await _eventRepo.GetEventByIdAsync(eventId);
                if (evt == null)
                {
                    if (IsAjaxRequest()) return Json(new { success = false, message = "Event does not exist." });
                    TempData["BookingError"] = "Event does not exist.";
                    return RedirectToAction("Index", "Event");
                }

                // ===== LẤY GIÁ VÉ TRƯỚC =====
                decimal pricePerTicket = evt.Price ?? 0m;

                // Fallback vào TblEvents nếu Price của ViewModel chưa map
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

                bool isFreeEvent = pricePerTicket <= 0m;

                // FREE EVENT: ép quantity = 1
                if (isFreeEvent)
                {
                    quantity = 1;
                }

                // ===== CHECK SỐ LƯỢNG CÒN LẠI =====
                var confirmed = await _bookingRepo.GetConfirmedSlotsForEventAsync(eventId);
                var available = Math.Max(0, evt.MaxSlot - confirmed);

                if (quantity > available)
                {
                    var msg = $"Not enough slots. Remaining {available} slot(s).";

                    if (IsAjaxRequest()) return Json(new { success = false, message = msg });
                    TempData["BookingError"] = msg;
                    return RedirectToAction("Register", new { id = eventId });
                }

                // ===== TOTAL COST =====
                var totalCost = pricePerTicket * quantity;

                // ===== LẤY USER ID (bắt buộc phải có) =====
                int? userId = null;
                var claim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(claim, out var uid)) userId = uid;

                if (!userId.HasValue)
                {
                    var loginUrl = Url.Action("Login", "Account", new
                    {
                        area = "Client",
                        returnUrl = Url.Action("Register", "EventBooking",
                            new { area = "Client", id = eventId })
                    });

                    if (IsAjaxRequest())
                        return Json(new { success = false, requiresLogin = true, loginUrl });

                    return Redirect(loginUrl);
                }

                var notes = $"ContactName:{contactName};ContactEmail:{contactEmail};Qty:{quantity}";

                // =====================================================================================
                // FREE EVENT - MỖI USER CHỈ ĐƯỢC 1 VÉ
                // =====================================================================================
                if (isFreeEvent)
                {
                    // check đã có vé free/paid cho event này chưa (status 1 hoặc 2)
                    var alreadyBooked = await _bookingRepo.HasConfirmedBookingForUserAsync(eventId, userId.Value);
                    if (alreadyBooked)
                    {
                        var msgFree = "You have already booked a free ticket for this event. Each account can only have one free ticket.";

                        if (IsAjaxRequest())
                            return Json(new { success = false, message = msgFree });

                        TempData["BookingError"] = msgFree;
                        return RedirectToAction("Register", new { id = eventId });
                    }

                    // tạo booking free
                    var booking = await _bookingRepo.CreateBookingAsync(
                        tenantId: evt.TenantPositionId,
                        userId: userId,
                        eventId: eventId,
                        totalCost: totalCost,    // 0
                        quantity: quantity,      // 1
                        notes: notes
                    );

                    // gửi email xác nhận
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _ticketEmailService.SendEventBookingSuccessEmailAsync(booking.EventBookingId);
                        }
                        catch
                        {
                        }
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
                // PAID EVENT - ĐẶT BAO NHIÊU VÉ CŨNG ĐƯỢC (THEO LIMIT SLOT)
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

                if (IsAjaxRequest()) return Json(new { success = false, message = "Server error." });

                TempData["BookingError"] = "System error while creating booking.";
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
                    return RedirectToAction("PaymentFailed", new { message = "Không nhận được phản hồi từ VNPAY" });

                int bookingId = 0;

                var parts = (response.OrderDescription ?? "")
                    .Split(';', StringSplitOptions.RemoveEmptyEntries);

                foreach (var p in parts)
                {
                    if (p.StartsWith("BookingId:", StringComparison.OrdinalIgnoreCase))
                        int.TryParse(p.Replace("BookingId:", "").Trim(), out bookingId);
                }

                // 🔴 kiểm tra mã phản hồi VNPAY
                var vnpResponseCode = Request.Query["vnp_ResponseCode"].ToString();
                if (string.IsNullOrEmpty(vnpResponseCode) || vnpResponseCode != "00")
                {
                    return RedirectToAction("PaymentFailed", new
                    {
                        message = $"Thanh toán thất bại (Mã lỗi: {vnpResponseCode})"
                    });
                }

                // ✅ thành công: đánh dấu paid
                var marked = await _bookingRepo.MarkBookingPaidAsync(bookingId);
                if (!marked)
                {
                    _logger.LogWarning("PaymentCallbackVnpay: MarkBookingPaidAsync({BookingId}) failed", bookingId);
                    return RedirectToAction("PaymentFailed", new { message = "Booking not found or payment status could not be updated." });
                }

                try
                {
                    await _ticketEmailService.SendEventBookingSuccessEmailAsync(bookingId);
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
                return RedirectToAction("PaymentFailed", new { message = "Có lỗi trong quá trình xử lý thanh toán." });
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
                (booking.EventBookingStatus ?? 1) == 0 ? "Cancelled" :
                (evtDetail.EndDate ?? evtDetail.StartDate) <= now ? "Completed" :
                "Upcoming";

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
            try
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdStr))
                    return Json(new { success = false, message = "You must be logged in." });

                int userId = int.Parse(userIdStr);

                var booking = await _bookingRepo.GetByIdAsync(bookingId);

                if (booking == null || booking.EventBookingUserId != userId)
                    return Json(new { success = false, message = "Booking not found." });

                var evt = booking.EventBookingEvent;
                if (evt == null)
                    return Json(new { success = false, message = "Event info could not be loaded." });

                // 1) Check 24h rule
                if (evt.EventStart - DateTime.Now < TimeSpan.FromHours(24))
                {
                    return Json(new
                    {
                        success = false,
                        message = "You can only cancel tickets at least 24 hours before the event starts."
                    });
                }

                // 2) Quantity checks
                int qty = booking.EventBookingQuantity ?? 0;

                if (qty <= 0)
                    return Json(new { success = false, message = "You have no active tickets to cancel." });

                if (cancelQuantity <= 0)
                    return Json(new { success = false, message = "Cancellation quantity must be greater than 0." });

                if (cancelQuantity > qty)
                    return Json(new { success = false, message = "Cancellation quantity exceeds remaining tickets." });

                int remainingQty = qty - cancelQuantity;
                decimal unitPrice = booking.EventBookingUnitPrice ?? 0;
                decimal refundAmount = unitPrice * cancelQuantity;

                // 3) Update booking (match SQL design)
                booking.EventBookingQuantity = remainingQty;
                booking.EventBookingTotalCost = remainingQty * unitPrice;

                // PaymentStatus: 1 = Paid, 2 = PartiallyRefunded, 3 = Cancelled
                booking.EventBookingPaymentStatus = remainingQty > 0 ? 2 : 3;

                // Status: 0 = Cancelled
                if (remainingQty == 0)
                {
                    booking.EventBookingStatus = 0;
                }

                // Append small note
                string smallLog =
                    $"[Cancel {DateTime.Now:dd/MM HH:mm}] Cancelled {cancelQuantity} ticket(s) (remaining {remainingQty}).";

                booking.EventBookingNotes =
                    string.IsNullOrWhiteSpace(booking.EventBookingNotes)
                        ? smallLog
                        : booking.EventBookingNotes + "\n" + smallLog;

                // 4) Write history row
                var history = new TblEventBookingHistory
                {
                    EventBookingHistoryBookingId = booking.EventBookingId,
                    EventBookingHistoryEventId = booking.EventBookingEventId,
                    EventBookingHistoryUserId = userId,
                    EventBookingHistoryAction = "PartialCancel",
                    EventBookingHistoryDetails =
                        $"User cancelled {cancelQuantity} ticket(s). From {qty} to {remainingQty}. Refund {refundAmount:N0}",
                    EventBookingHistoryRelatedDate = DateOnly.FromDateTime(DateTime.Now),
                    EventBookingHistoryQuantity = cancelQuantity,
                    EventBookingHistoryCreatedAt = DateTime.Now
                };

                _context.TblEventBookingHistories.Add(history);
                _context.Update(booking);
                await _context.SaveChangesAsync();

                // 5) Send email
                try
                {
                    var evtDetail = await _eventRepo.GetEventByIdAsync(booking.EventBookingEventId, userId);
                    string eventName = evtDetail?.Title ?? "Event";

                    await _ticketEmailService.SendEventCancelEmailAsync(
                        userId: booking.EventBookingUserId,
                        eventName: eventName,
                        bookingId: booking.EventBookingId,
                        cancelledQty: cancelQuantity,
                        remainingQty: remainingQty,
                        refundAmount: refundAmount
                    );
                }
                catch (Exception exMail)
                {
                    _logger.LogError(exMail, "Error sending event cancel email for booking {BookingId}", bookingId);
                }

                return Json(new
                {
                    success = true,
                    message = $"Cancelled {cancelQuantity} ticket(s), refund {refundAmount:N0}₫."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PartialCancel error");
                return Json(new
                {
                    success = false,
                    message = "Error while processing your cancellation request."
                });
            }
        }
    }
}
