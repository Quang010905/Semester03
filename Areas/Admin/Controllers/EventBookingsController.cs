using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering; // For SelectList
using Microsoft.EntityFrameworkCore;
using Semester03.Models.Entities;
using Semester03.Models.Repositories;
using Semester03.Services.Email;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Semester03.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "1")]
    public class EventBookingsController : Controller
    {
        private readonly EventBookingRepository _bookingRepo;
        private readonly EventRepository _eventRepo; // Added to fetch Event details
        private readonly AbcdmallContext _context;
        private readonly IEmailSender _emailSender;

        public EventBookingsController(EventBookingRepository bookingRepo, EventRepository eventRepo, AbcdmallContext context, IEmailSender emailSender)
        {
            _bookingRepo = bookingRepo;
            _eventRepo = eventRepo;
            _context = context;
            _emailSender = emailSender;
        }

        // GET: Admin/EventBookings
        public async Task<IActionResult> Index(
            int? eventId,
            string search,
            DateTime? fromDate,
            DateTime? toDate,
            string status)
        {
            // 1. Load Dropdown Event
            var allEvents = await _eventRepo.GetAllAsync();
            ViewData["EventList"] = new SelectList(allEvents, "EventId", "EventName", eventId);

            // 2. Gọi hàm Search thay vì GetAll
            var bookings = await _bookingRepo.SearchBookingsAsync(eventId, search, fromDate, toDate, status);

            // 3. Tính toán thống kê (trên dữ liệu đã lọc)
            if (eventId.HasValue)
            {
                // ... (Logic thống kê cũ giữ nguyên, nhưng tính revenue trên list bookings mới lọc) ...
                var selectedEvent = await _eventRepo.GetByIdAdminAsync(eventId.Value);
                if (selectedEvent != null)
                {
                    ViewData["EventName"] = selectedEvent.EventName;
                    ViewData["MaxSlots"] = selectedEvent.EventMaxSlot;
                    // Note: SoldSlots nên lấy tổng thực tế của Event, ko bị ảnh hưởng bởi filter ngày
                    ViewData["SoldSlots"] = await _bookingRepo.GetConfirmedSlotsForEventAsync(eventId.Value);

                    // Revenue tính theo danh sách đang hiển thị
                    ViewData["Revenue"] = bookings.Where(b => b.EventBookingPaymentStatus == 1).Sum(b => b.EventBookingTotalCost ?? 0);
                }
                ViewData["Title"] = $"Manage: {selectedEvent?.EventName}";
                ViewData["EventFilter"] = eventId.Value;
            }
            else
            {
                ViewData["Title"] = "Event Booking Management";
                ViewData["TotalBookings"] = bookings.Count();
                ViewData["TotalRevenue"] = bookings.Where(b => b.EventBookingPaymentStatus == 1).Sum(b => b.EventBookingTotalCost ?? 0);
            }

            // 4. Lưu giá trị filter để hiện lại trên View
            ViewData["CurrentSearch"] = search;
            ViewData["FromDate"] = fromDate?.ToString("yyyy-MM-dd");
            ViewData["ToDate"] = toDate?.ToString("yyyy-MM-dd");
            ViewData["CurrentStatus"] = status;

            return View(bookings);
        }

        // GET: Admin/EventBookings/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var booking = await _bookingRepo.GetByIdWithHistoryAsync(id.Value);
            if (booking == null) return NotFound();
            return View(booking);
        }

        // POST: Admin/EventBookings/UpdateStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, int paymentStatus)
        {
            int adminId = 1;
            var result = await _bookingRepo.UpdateStatusByAdminAsync(id, paymentStatus, adminId);

            if (result) TempData["Success"] = "Booking status updated successfully.";
            else TempData["Error"] = "Could not update booking status.";

            return RedirectToAction(nameof(Details), new { id = id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id, string reason)
        {
            var booking = await _context.TblEventBookings
                .Include(b => b.EventBookingEvent)
                .Include(b => b.EventBookingUser)
                .FirstOrDefaultAsync(b => b.EventBookingId == id);

            if (booking == null) return NotFound();

            
            if (booking.EventBookingEvent != null && booking.EventBookingEvent.EventStart <= DateTime.Now)
            {
                TempData["Error"] = "Cannot cancel: The event has already started or finished.";
                return RedirectToAction(nameof(Details), new { id = id });
            }

             
            // Status 3 = Cancelled
            bool result = await _bookingRepo.CancelBookingAsync(id, 3);

            if (result)
            {
                // C. Send Email Notification to User
                if (booking.EventBookingUser != null && !string.IsNullOrEmpty(booking.EventBookingUser.UsersEmail))
                {
                    string subject = $"[ABCD Mall] Booking Cancellation - #{booking.EventBookingId}";
                    string body = $@"
                        <h3 style='color:red;'>Booking Cancelled by Admin</h3>
                        <p>Dear <strong>{booking.EventBookingUser.UsersFullName}</strong>,</p>
                        <p>We regret to inform you that your booking for event <strong>{booking.EventBookingEvent?.EventName}</strong> has been cancelled by the administrator.</p>
                        
                        <div style='background-color:#fff3cd; padding:10px; border-left: 5px solid #ffc107; margin: 10px 0;'>
                            <strong>Reason:</strong> {reason ?? "Unforeseen circumstances"}
                        </div>

                        <p>If you have already made a payment, please contact our support desk for a refund procedure.</p>
                        <br/>
                        <p>Best Regards,<br/>ABCD Mall Management</p>";

                    try
                    {
                        await _emailSender.SendEmailAsync(booking.EventBookingUser.UsersEmail, subject, body);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Email sending failed: " + ex.Message);
                    }
                }

                TempData["Success"] = "Booking cancelled successfully. Notification email has been sent.";
            }
            else
            {
                TempData["Error"] = "Failed to cancel booking. It might create conflicting data.";
            }

            return RedirectToAction(nameof(Details), new { id = id });
        }

        [HttpPost]
        [HttpPost]
        public async Task<IActionResult> ExportEventSummary(
    int? eventId,
    string search,
    DateTime? fromDate,
    DateTime? toDate,
    string status)
        {
            // 1. Lấy dữ liệu (theo bộ lọc)
            var bookings = await _bookingRepo.SearchBookingsAsync(eventId, search, fromDate, toDate, status);

            // [FIX] Khai báo và lấy giá trị cho sheetName
            string sheetName = "All Events";
            if (eventId.HasValue)
            {
                var evt = await _eventRepo.GetByIdAdminAsync(eventId.Value);
                sheetName = evt?.EventName ?? "Unknown Event";
            }

            // Lấy dữ liệu báo cáo
            var reportData = bookings.Select(b => new
            {
                BookingId = b.EventBookingId,
                EventName = b.EventBookingEvent?.EventName,
                Customer = b.EventBookingUser?.UsersFullName ?? "Unknown",
                Email = b.EventBookingUser?.UsersEmail,
                Phone = b.EventBookingUser?.UsersPhone,
                Quantity = b.EventBookingQuantity ?? 0,
                TotalCost = b.EventBookingTotalCost ?? 0,
                Status = b.EventBookingPaymentStatus == 1 ? "Paid" :
                         (b.EventBookingPaymentStatus == 3 ? "Cancelled" : "Unpaid"),
                Date = b.EventBookingCreatedDate?.ToString("dd/MM/yyyy HH:mm")
            }).ToList();

            // 2. Tạo File Excel
            using (var workbook = new XLWorkbook())
            {
                var ws = workbook.Worksheets.Add("Booking List");

                // Tiêu đề
                ws.Cell(1, 1).Value = "EVENT BOOKING REPORT";
                ws.Range(1, 1, 1, 9).Merge().Style.Font.Bold = true; // Merge 9 cột cho đẹp
                ws.Range(1, 1, 1, 9).Style.Font.FontSize = 14;
                ws.Range(1, 1, 1, 9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // [FIX] Sử dụng biến sheetName đã khai báo ở trên
                ws.Cell(2, 1).Value = $"Filter: {sheetName}";
                ws.Cell(2, 1).Style.Font.Italic = true;

                // Header
                var headers = new[] { "ID", "Event Name", "Customer", "Email", "Phone", "Qty", "Total (VND)", "Status", "Date" };
                for (int i = 0; i < headers.Length; i++)
                {
                    ws.Cell(4, i + 1).Value = headers[i];
                }
                ws.Range(4, 1, 4, 9).Style.Font.Bold = true;
                ws.Range(4, 1, 4, 9).Style.Fill.BackgroundColor = XLColor.Orange;
                ws.Range(4, 1, 4, 9).Style.Font.FontColor = XLColor.White;

                // Data
                int row = 5;
                foreach (var item in reportData)
                {
                    ws.Cell(row, 1).Value = item.BookingId;
                    ws.Cell(row, 2).Value = item.EventName;
                    ws.Cell(row, 3).Value = item.Customer;
                    ws.Cell(row, 4).Value = item.Email;
                    ws.Cell(row, 5).Value = item.Phone;
                    ws.Cell(row, 6).Value = item.Quantity;
                    ws.Cell(row, 7).Value = item.TotalCost;
                    ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0";
                    ws.Cell(row, 8).Value = item.Status;

                    // Tô màu trạng thái
                    if (item.Status == "Paid") ws.Cell(row, 8).Style.Font.FontColor = XLColor.Green;
                    else if (item.Status == "Cancelled") ws.Cell(row, 8).Style.Font.FontColor = XLColor.Red;

                    ws.Cell(row, 9).Value = item.Date;
                    row++;
                }

                ws.Columns().AdjustToContents();

                // 3. Xuất file
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();
                    string fileName = $"EventBookings_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
                    return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
            }
        }
    }
}