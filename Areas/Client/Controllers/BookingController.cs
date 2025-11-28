using Microsoft.AspNetCore.Mvc;
using Semester03.Areas.Client.Models.ViewModels;
using Semester03.Models.Repositories;
using Semester03.Models.Entities;
using Semester03.Services.Vnpay;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Semester03.Services.Email;

namespace Semester03.Areas.Client.Controllers
{
    [Area("Client")]
    [Route("Client/[controller]/[action]")]
    public class BookingController : ClientBaseController
    {
        private readonly ShowtimeRepository _showRepo;
        private readonly MovieRepository _movieRepo;
        private readonly SeatRepository _seatRepo;
        private readonly AbcdmallContext _context;
        private readonly IVnPayService _vnPayService;
        private readonly ILogger<BookingController> _logger;
        private readonly TicketEmailService _ticketEmailService;

        public BookingController(
            TenantTypeRepository tenantTypeRepo,
            ShowtimeRepository showRepo,
            MovieRepository movieRepo,
            SeatRepository seatRepo,
            IVnPayService vnPayService,
            AbcdmallContext context,
            ILogger<BookingController> logger,
            TicketEmailService ticketEmailService
        ) : base(tenantTypeRepo)
        {
            _showRepo = showRepo;
            _movieRepo = movieRepo;
            _seatRepo = seatRepo;
            _vnPayService = vnPayService;
            _context = context;
            _logger = logger;
            _ticketEmailService = ticketEmailService;
        }

        [HttpGet]
        public IActionResult BookTicket(int movieId)
        {
            var movie = _movieRepo.GetMovieCard(movieId);
            if (movie == null) return NotFound();

            var today = DateTime.Now.Date;
            var days = Enumerable.Range(0, 7).Select(i => today.AddDays(i)).ToList();

            var vm = new BookTicketVm
            {
                Movie = movie,
                WeekDays = days.Select(d => new DayVm
                {
                    Date = d,
                    Display = d.ToString("ddd dd MMM")
                }).ToList(),
                SelectedDate = today
            };

            return View(vm);
        }

        [HttpGet]
        public IActionResult GetShowtimes(int movieId, string date)
        {
            if (!DateTime.TryParse(date, out var dt))
                return BadRequest("Invalid date");

            var list = _showRepo.GetShowtimesForMovieOnDate(movieId, dt);

            // 🔥 CHẶN SUẤT CHIẾU ĐÃ QUA GIỜ (CHỈ ÁP DỤNG CHO NGÀY HÔM NAY)
            if (dt.Date == DateTime.Now.Date)
            {
                var now = DateTime.Now;
                list = list
                    .Where(s => s.StartTime > now)
                    .ToList();
            }

            return PartialView("_ShowtimeGrid", list);
        }

        [HttpGet]
        public IActionResult SelectSeat(int showtimeId)
        {
            var vm = _seatRepo.GetSeatLayoutForShowtime(showtimeId);
            if (vm == null || vm.Seats == null)
                return NotFound();

            return View(vm);
        }

        [HttpPost]
        public IActionResult ReserveSeats([FromBody] ReserveRequestVm req)
        {
            if (req == null || req.ShowtimeSeatIds == null || !req.ShowtimeSeatIds.Any())
                return BadRequest(new { success = false, message = "No seats selected" });

            int? userId = null;
            if (User?.Identity?.IsAuthenticated == true)
            {
                var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(claim, out var id)) userId = id;
            }

            var (succeeded, failed) = _seatRepo.ReserveSeats(req.ShowtimeId, req.ShowtimeSeatIds, userId);

            var allIds = succeeded.Concat(failed).ToList();

            var showtimeSeatRows = _context.TblShowtimeSeats
                .Where(ss => allIds.Contains(ss.ShowtimeSeatId))
                .Select(ss => new { ss.ShowtimeSeatId, ss.ShowtimeSeatSeatId })
                .ToList();

            var seatIds = showtimeSeatRows.Select(x => x.ShowtimeSeatSeatId).Distinct().ToList();
            var seats = _context.TblSeats
                .Where(s => seatIds.Contains(s.SeatId))
                .Select(s => new { s.SeatId, s.SeatLabel })
                .ToList();

            var mapping = showtimeSeatRows
                .Join(seats,
                      ss => ss.ShowtimeSeatSeatId,
                      s => s.SeatId,
                      (ss, s) => new { ss.ShowtimeSeatId, SeatLabel = s.SeatLabel })
                .ToList();

            var succeededLabels = mapping.Where(m => succeeded.Contains(m.ShowtimeSeatId))
                                        .Select(m => m.SeatLabel).ToList();
            var failedLabels = mapping.Where(m => failed.Contains(m.ShowtimeSeatId))
                                      .Select(m => m.SeatLabel).ToList();

            var refreshed = _seatRepo.RefreshSeatLayout(req.ShowtimeId);

            return Ok(new
            {
                success = succeeded.Any(),
                succeeded,
                failed,
                succeededLabels,
                failedLabels,
                layout = refreshed
            });
        }

        [HttpGet]
        public IActionResult ConfirmBooking(int showtimeId, string seatIds)
        {
            var seatIdList = (seatIds ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => int.TryParse(s.Trim(), out var v) ? v : 0)
                .Where(v => v > 0)
                .ToList();

            if (!seatIdList.Any())
                return BadRequest("Không có ghế hợp lệ.");

            var showtime = _context.TblShowtimes
                .Include(s => s.ShowtimeMovie)
                .Include(s => s.ShowtimeScreen)
                    .ThenInclude(sc => sc.ScreenCinema)
                .FirstOrDefault(s => s.ShowtimeId == showtimeId);

            if (showtime == null)
                return NotFound();

            var showtimeSeatRows = _context.TblShowtimeSeats
                .Where(ss => seatIdList.Contains(ss.ShowtimeSeatId) && ss.ShowtimeSeatShowtimeId == showtimeId)
                .Select(ss => new { ss.ShowtimeSeatId, ss.ShowtimeSeatSeatId })
                .ToList();

            var seatIdsDistinct = showtimeSeatRows.Select(x => x.ShowtimeSeatSeatId).Distinct().ToList();
            var seats = _context.TblSeats
                .Where(s => seatIdsDistinct.Contains(s.SeatId))
                .Select(s => new { s.SeatId, s.SeatLabel })
                .ToList();

            var seatLabels = showtimeSeatRows
                .Join(seats,
                      ss => ss.ShowtimeSeatSeatId,
                      s => s.SeatId,
                      (ss, s) => s.SeatLabel)
                .ToList();

            var vm = new BookingConfirmVm
            {
                ShowtimeId = showtime.ShowtimeId,
                MovieTitle = showtime.ShowtimeMovie?.MovieTitle ?? "",
                CinemaName = showtime.ShowtimeScreen?.ScreenCinema?.CinemaName ?? "",
                ScreenName = showtime.ShowtimeScreen?.ScreenName ?? "",
                ShowtimeStart = showtime.ShowtimeStart,
                SelectedSeats = seatLabels,
                SeatPrice = showtime.ShowtimePrice,
                TotalAmount = showtime.ShowtimePrice * seatLabels.Count
            };

            // --- Load coupon đang active & trong thời gian ---
            var now = DateTime.Now;
            var coupons = _context.TblCoupons
                .Where(c => !((c.CouponIsActive ?? false) == false
                              || c.CouponValidFrom > now
                              || c.CouponValidTo < now))
                .Select(c => new CouponDto
                {
                    Id = c.CouponId,
                    Name = c.CouponName,
                    DiscountPercent = c.CouponDiscountPercent,
                    MinimumPointsRequired = c.CouponMinimumPointsRequired,
                    ValidFrom = c.CouponValidFrom,
                    ValidTo = c.CouponValidTo,
                    IsActive = c.CouponIsActive ?? false
                })
                .ToList();

            vm.AvailableCoupons = coupons;

            // user points (nếu đăng nhập)
            int userPoints = 0;
            if (User?.Identity?.IsAuthenticated == true)
            {
                var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(claim, out var userId))
                {
                    var u = _context.TblUsers.FirstOrDefault(x => x.UsersId == userId);
                    if (u != null) userPoints = u.UsersPoints ?? 0;
                }
            }
            vm.UserPoints = userPoints;

            return View("ConfirmBooking", vm);
        }

        // =========================
        //  TẠO URL VNPAY (TÍNH GIẢM GIÁ Ở SERVER)
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePaymentUrlVnpay(
            [FromForm] int showtimeId,
            [FromForm] string seatIds,
            [FromForm] decimal amount,
            [FromForm] int? couponId)
        {
            try
            {
                int? buyerId = null;
                if (User?.Identity?.IsAuthenticated == true)
                {
                    var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                    if (int.TryParse(claim, out var id)) buyerId = id;
                }

                var showtimeSeatIdList = (seatIds ?? "")
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => int.TryParse(s.Trim(), out var v) ? v : 0)
                    .Where(v => v > 0)
                    .ToList();

                if (!showtimeSeatIdList.Any())
                    return Json(new { success = false, message = "Không có ghế hợp lệ để thanh toán." });

                var showtime = await _context.TblShowtimes
                    .FirstOrDefaultAsync(s => s.ShowtimeId == showtimeId);

                if (showtime == null)
                    return Json(new { success = false, message = "Suất chiếu không tồn tại." });

                decimal originalTotal = showtime.ShowtimePrice * showtimeSeatIdList.Count;

                decimal discountAmount = 0m;
                decimal finalAmount = originalTotal;

                if (couponId.HasValue && couponId.Value > 0)
                {
                    var coupon = await _context.TblCoupons
                        .FirstOrDefaultAsync(c => c.CouponId == couponId.Value && (c.CouponIsActive ?? false));
                    if (coupon == null)
                        return Json(new { success = false, message = "Mã giảm giá không hợp lệ." });

                    var now = DateTime.Now;
                    if (coupon.CouponValidFrom > now || coupon.CouponValidTo < now)
                        return Json(new { success = false, message = "Mã đã hết hạn hoặc chưa hiệu lực." });

                    if (!buyerId.HasValue)
                        return Json(new { success = false, message = "Vui lòng đăng nhập để sử dụng mã giảm giá." });

                    var user = await _context.TblUsers.FirstOrDefaultAsync(u => u.UsersId == buyerId.Value);
                    if (user == null)
                        return Json(new { success = false, message = "Người dùng không tồn tại." });

                    if (coupon.CouponMinimumPointsRequired.HasValue &&
                        (user.UsersPoints ?? 0) < coupon.CouponMinimumPointsRequired.Value)
                        return Json(new { success = false, message = "You don't have enough points to use this code." });

                    var used = await _context.TblCouponUsers
                        .AnyAsync(x => x.CouponId == couponId.Value && x.UsersId == buyerId.Value);
                    if (used)
                        return Json(new { success = false, message = "You have already used this code." });

                    discountAmount = Math.Floor(originalTotal * (coupon.CouponDiscountPercent / 100m));
                    finalAmount = Math.Max(0m, originalTotal - discountAmount);
                }

                var model = new Semester03.Areas.Client.Models.Vnpay.PaymentInformationModel
                {
                    OrderType = "movie-ticket",
                    Amount = (double)finalAmount,
                    OrderDescription =
                        $"Showtime:{showtimeId};" +
                        $"Seats:{string.Join(",", showtimeSeatIdList)};" +
                        $"Original:{originalTotal.ToString(CultureInfo.InvariantCulture)};" +
                        $"Discount:{discountAmount.ToString(CultureInfo.InvariantCulture)};" +
                        $"Final:{finalAmount.ToString(CultureInfo.InvariantCulture)};" +
                        $"Coupon:{(couponId.HasValue ? couponId.Value.ToString() : "")}",
                    Name = $"Booking for Showtime {showtimeId}"
                };

                var url = _vnPayService.CreatePaymentUrl(model, HttpContext);

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = true, url });

                return Redirect(url);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating VNPAY URL");
                return Json(new { success = false, message = "Lỗi tạo URL VNPAY: " + ex.Message });
            }
        }

        // =========================
        //  CALLBACK VNPAY
        // =========================
        [HttpGet]
        public IActionResult PaymentCallbackVnpay()
        {
            var response = _vnPayService.PaymentExecute(Request.Query);
            if (response == null)
                return RedirectToAction("PaymentFailed", new { message = "Không nhận được phản hồi từ VNPAY" });

            int showtimeId = 0;
            string seatIds = "";
            int? couponId = null;

            decimal originalAmount = 0m;
            decimal discountAmount = 0m;
            decimal finalAmount = 0m;

            var parts = (response.OrderDescription ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries);
            foreach (var p in parts)
            {
                var trimmed = p.Trim();
                if (trimmed.StartsWith("Showtime:", StringComparison.OrdinalIgnoreCase))
                    int.TryParse(trimmed.Substring("Showtime:".Length).Trim(), out showtimeId);
                else if (trimmed.StartsWith("Seats:", StringComparison.OrdinalIgnoreCase))
                    seatIds = trimmed.Substring("Seats:".Length).Trim();
                else if (trimmed.StartsWith("Original:", StringComparison.OrdinalIgnoreCase))
                    decimal.TryParse(trimmed.Substring("Original:".Length).Trim(),
                        NumberStyles.Any, CultureInfo.InvariantCulture, out originalAmount);
                else if (trimmed.StartsWith("Discount:", StringComparison.OrdinalIgnoreCase))
                    decimal.TryParse(trimmed.Substring("Discount:".Length).Trim(),
                        NumberStyles.Any, CultureInfo.InvariantCulture, out discountAmount);
                else if (trimmed.StartsWith("Final:", StringComparison.OrdinalIgnoreCase))
                    decimal.TryParse(trimmed.Substring("Final:".Length).Trim(),
                        NumberStyles.Any, CultureInfo.InvariantCulture, out finalAmount);
                else if (trimmed.StartsWith("Coupon:", StringComparison.OrdinalIgnoreCase))
                {
                    var raw = trimmed.Substring("Coupon:".Length).Trim();
                    if (int.TryParse(raw, out var c)) couponId = c;
                }
            }

            // 🔴 kiểm tra trực tiếp mã phản hồi của VNPAY
            var vnpResponseCode = Request.Query["vnp_ResponseCode"].ToString();

            if (string.IsNullOrEmpty(vnpResponseCode) || vnpResponseCode != "00")
            {
                // user hủy / lỗi thanh toán
                return RedirectToAction("PaymentFailed", new
                {
                    showtimeId,
                    seatIds,
                    message = $"Thanh toán thất bại (Mã lỗi: {vnpResponseCode})"
                });
            }

            // ✅ thành công
            return RedirectToAction("PaymentSuccess", new
            {
                showtimeId,
                seatIds,
                couponId,
                originalAmount,
                discountAmount,
                finalAmount
            });
        }


        // =========================
        //  PAYMENT SUCCESS
        // =========================
        [HttpGet]
        public async Task<IActionResult> PaymentSuccess(
    int showtimeId,
    string seatIds,
    int? couponId,
    decimal originalAmount = 0m,
    decimal discountAmount = 0m,
    decimal finalAmount = 0m)
        {
            var rawParts = (seatIds ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();

            var parsedIds = rawParts.Select(s => int.TryParse(s, out var v) ? v : 0)
                                    .Where(v => v > 0).ToList();
            List<int> showtimeSeatIds = new List<int>();

            try
            {
                // ====== Xử lý seatIds như cũ ======
                if (parsedIds.Any())
                {
                    showtimeSeatIds = parsedIds;
                }
                else if (rawParts.Any())
                {
                    var seatEntities = await _context.TblSeats
                        .Where(s => rawParts.Contains(s.SeatLabel))
                        .Select(s => new { s.SeatId })
                        .ToListAsync();

                    var seatIdList = seatEntities.Select(s => s.SeatId).ToList();

                    if (seatIdList.Any())
                    {
                        var q = _context.TblShowtimeSeats.AsQueryable();
                        q = q.Where(ss => seatIdList.Contains(ss.ShowtimeSeatSeatId));
                        if (showtimeId > 0) q = q.Where(ss => ss.ShowtimeSeatShowtimeId == showtimeId);

                        showtimeSeatIds = await q.Select(ss => ss.ShowtimeSeatId).ToListAsync();
                    }
                }

                if (!showtimeSeatIds.Any())
                {
                    ViewData["SeatLabels"] = new List<string>();
                    ViewData["ShowtimeId"] = showtimeId;
                    ViewData["OriginalAmount"] = 0m;
                    ViewData["DiscountAmount"] = 0m;
                    ViewData["TotalAmount"] = 0m;
                    ViewData["PricePerSeat"] = 0m;
                    ViewData["ShowtimeSeatIds"] = "";
                    ViewData["PointsAwarded"] = 0;
                    return View();
                }

                if (showtimeId <= 0)
                {
                    var possibleShowtimeIds = await _context.TblShowtimeSeats
                        .Where(ss => showtimeSeatIds.Contains(ss.ShowtimeSeatId))
                        .Select(ss => ss.ShowtimeSeatShowtimeId)
                        .Distinct()
                        .ToListAsync();

                    if (possibleShowtimeIds.Any())
                    {
                        showtimeId = possibleShowtimeIds.First();
                    }
                }

                var showtime = await _context.TblShowtimes
                    .FirstOrDefaultAsync(s => s.ShowtimeId == showtimeId);
                decimal pricePerSeat = showtime?.ShowtimePrice ?? 0m;

                int? buyerId = null;
                var claim = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrWhiteSpace(claim) && int.TryParse(claim, out var parsedBuyer))
                {
                    buyerId = parsedBuyer;
                    _logger.LogInformation("PaymentSuccess: buyerId from claim = {BuyerId}", buyerId);
                }
                else
                {
                    if (Request.Cookies.TryGetValue("GigaMall_PendingBuyerId", out var pendingIdStr)
                        && int.TryParse(pendingIdStr, out var pendingId))
                    {
                        buyerId = pendingId;
                        _logger.LogInformation("PaymentSuccess: buyerId from GigaMall_PendingBuyerId cookie = {BuyerId}", buyerId);

                        Response.Cookies.Append("GigaMall_PendingBuyerId", "",
                            new Microsoft.AspNetCore.Http.CookieOptions
                            {
                                Expires = DateTimeOffset.UtcNow.AddDays(-1),
                                Path = "/"
                            });
                    }
                    else if (Request.Cookies.TryGetValue("GigaMall_LastUserId", out var lastUserIdStr)
                             && int.TryParse(lastUserIdStr, out var lastUserId))
                    {
                        buyerId = lastUserId;
                        _logger.LogInformation("PaymentSuccess: buyerId from GigaMall_LastUserId cookie = {BuyerId}", buyerId);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "PaymentSuccess: No buyerId found via claim or cookies. Email will not be sent automatically.");
                    }
                }

                var seatMappingsResult = new List<(int ShowtimeSeatId, string SeatLabel)>();

                decimal effectiveOriginalAmount = originalAmount;
                decimal effectiveFinalAmount = finalAmount;
                decimal effectiveDiscountAmount = discountAmount;
                int pointsAwarded = 0;

                // ================== PHẦN DÙNG ADO.NET – ĐÃ SỬA ==================
                var conn = _context.Database.GetDbConnection();
                if (conn.State != ConnectionState.Open)
                    await conn.OpenAsync();       // KHÔNG using / Dispose conn – EF vẫn giữ lifetime

                await using (var dbTx = await conn.BeginTransactionAsync())
                {
                    try
                    {
                        string inClause = string.Join(",", showtimeSeatIds);

                        var showtimeSeatRows = new List<(int ShowtimeSeatId, int SeatId, int ShowtimeId)>();
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.Transaction = dbTx;
                            cmd.CommandType = CommandType.Text;
                            cmd.CommandText = $@"
                                    SELECT ShowtimeSeat_ID, ShowtimeSeat_SeatID, ShowtimeSeat_ShowtimeID
                                    FROM dbo.Tbl_ShowtimeSeat
                                    WHERE ShowtimeSeat_ID IN ({inClause})
                                ";
                            using var reader = await cmd.ExecuteReaderAsync();
                            while (await reader.ReadAsync())
                            {
                                var ssid = reader.GetInt32(0);
                                var seatId = reader.GetInt32(1);
                                var stid = reader.GetInt32(2);
                                showtimeSeatRows.Add((ssid, seatId, stid));
                            }
                            reader.Close();
                        }

                        if (!showtimeSeatRows.Any())
                        {
                            await dbTx.RollbackAsync();
                            ViewData["SeatLabels"] = new List<string>();
                            ViewData["ShowtimeId"] = showtimeId;
                            ViewData["OriginalAmount"] = 0m;
                            ViewData["DiscountAmount"] = 0m;
                            ViewData["TotalAmount"] = 0m;
                            ViewData["PricePerSeat"] = pricePerSeat;
                            ViewData["ShowtimeSeatIds"] = "";
                            ViewData["PointsAwarded"] = 0;
                            return View();
                        }

                        var seatIdDistinct = showtimeSeatRows.Select(x => x.SeatId).Distinct().ToList();
                        var seatIdListStr = string.Join(",", seatIdDistinct);
                        var seatLabelsById = new Dictionary<int, string>();
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.Transaction = dbTx;
                            cmd.CommandType = CommandType.Text;
                            cmd.CommandText = $@"
                                    SELECT Seat_ID, Seat_Label
                                    FROM dbo.Tbl_Seat
                                    WHERE Seat_ID IN ({seatIdListStr})
                                ";
                            using var reader = await cmd.ExecuteReaderAsync();
                            while (await reader.ReadAsync())
                            {
                                var id = reader.GetInt32(0);
                                var lbl = reader.IsDBNull(1) ? "" : reader.GetString(1);
                                seatLabelsById[id] = lbl;
                            }
                            reader.Close();
                        }

                        var seatMappings = showtimeSeatRows
                            .Select(r => new
                            {
                                r.ShowtimeSeatId,
                                Label = seatLabelsById.ContainsKey(r.SeatId) ? seatLabelsById[r.SeatId] : "",
                                r.ShowtimeId
                            })
                            .ToList();

                        var labels = seatMappings.Select(m => m.Label).ToList();
                        var seatCount = labels.Count;
                        var baseTotal = pricePerSeat * seatCount;

                        if (effectiveOriginalAmount <= 0) effectiveOriginalAmount = baseTotal;
                        if (effectiveFinalAmount <= 0) effectiveFinalAmount = baseTotal;
                        if (effectiveDiscountAmount <= 0) effectiveDiscountAmount = effectiveOriginalAmount - effectiveFinalAmount;
                        if (effectiveDiscountAmount < 0) effectiveDiscountAmount = 0m;

                        seatMappingsResult = seatMappings
                            .Select(m => (m.ShowtimeSeatId, m.Label))
                            .ToList();

                        var existingTicketSeatIds = new HashSet<int>();
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.Transaction = dbTx;
                            cmd.CommandType = CommandType.Text;
                            cmd.CommandText = $@"
                                    SELECT Ticket_ShowtimeSeatID
                                    FROM dbo.Tbl_Ticket
                                    WHERE Ticket_ShowtimeSeatID IN ({inClause})
                                ";
                            using var reader = await cmd.ExecuteReaderAsync();
                            while (await reader.ReadAsync())
                            {
                                if (!reader.IsDBNull(0))
                                {
                                    existingTicketSeatIds.Add(reader.GetInt32(0));
                                }
                            }
                            reader.Close();
                        }

                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.Transaction = dbTx;
                            cmd.CommandType = CommandType.Text;
                            cmd.CommandText = $@"
                                    UPDATE dbo.Tbl_ShowtimeSeat
                                    SET ShowtimeSeat_Status = @status,
                                        ShowtimeSeat_UpdatedAt = @now,
                                        ShowtimeSeat_ReservedByUserID = COALESCE(@reservedBy, ShowtimeSeat_ReservedByUserID),
                                        ShowtimeSeat_ReservedAt = COALESCE(@reservedAt, ShowtimeSeat_ReservedAt)
                                    WHERE ShowtimeSeat_ID IN ({inClause})
                                ";

                            var pStatus = cmd.CreateParameter();
                            pStatus.ParameterName = "@status";
                            pStatus.Value = "sold";
                            cmd.Parameters.Add(pStatus);

                            var pNow = cmd.CreateParameter();
                            pNow.ParameterName = "@now";
                            pNow.Value = DateTime.Now;
                            cmd.Parameters.Add(pNow);

                            var pReservedBy = cmd.CreateParameter();
                            pReservedBy.ParameterName = "@reservedBy";
                            pReservedBy.Value = (object?)buyerId ?? DBNull.Value;
                            cmd.Parameters.Add(pReservedBy);

                            var pReservedAt = cmd.CreateParameter();
                            pReservedAt.ParameterName = "@reservedAt";
                            pReservedAt.Value = (object?)DateTime.Now ?? DBNull.Value;
                            cmd.Parameters.Add(pReservedAt);

                            await cmd.ExecuteNonQueryAsync();
                        }

                        var toInsert = seatMappings
                            .Where(m => !existingTicketSeatIds.Contains(m.ShowtimeSeatId))
                            .ToList();

                        if (toInsert.Any())
                        {
                            var purchasedAt = DateTime.Now;

                            foreach (var m in toInsert)
                            {
                                var qrPayload =
                                    $"TICKET|ST={showtimeId}|SS={m.ShowtimeSeatId}|SEAT={m.Label}|U={buyerId?.ToString() ?? "GUEST"}|TS={DateTime.UtcNow:yyyyMMddHHmmss}";

                                using var cmd = conn.CreateCommand();
                                cmd.Transaction = dbTx;
                                cmd.CommandType = CommandType.Text;
                                cmd.CommandText = @"
                                        INSERT INTO dbo.Tbl_Ticket
                                            (Ticket_ShowtimeSeatID,
                                             Ticket_BuyerUserID,
                                             Ticket_Status,
                                             Ticket_Price,
                                             Ticket_CreatedAt,
                                             Ticket_QR)
                                        VALUES
                                            (@showtimeSeatId,
                                             @buyerId,
                                             @status,
                                             @price,
                                             @purchasedAt,
                                             @qrCode)
                                    ";

                                var pShowtimeSeatId = cmd.CreateParameter();
                                pShowtimeSeatId.ParameterName = "@showtimeSeatId";
                                pShowtimeSeatId.Value = m.ShowtimeSeatId;
                                cmd.Parameters.Add(pShowtimeSeatId);

                                var pBuyerId = cmd.CreateParameter();
                                pBuyerId.ParameterName = "@buyerId";
                                pBuyerId.Value = (object?)buyerId ?? DBNull.Value;
                                cmd.Parameters.Add(pBuyerId);

                                var pStatus2 = cmd.CreateParameter();
                                pStatus2.ParameterName = "@status";
                                pStatus2.Value = "sold";
                                cmd.Parameters.Add(pStatus2);

                                var pPrice = cmd.CreateParameter();
                                pPrice.ParameterName = "@price";
                                pPrice.Value = (object)pricePerSeat ?? DBNull.Value;
                                cmd.Parameters.Add(pPrice);

                                var pPurchasedAt = cmd.CreateParameter();
                                pPurchasedAt.ParameterName = "@purchasedAt";
                                pPurchasedAt.Value = purchasedAt;
                                cmd.Parameters.Add(pPurchasedAt);

                                var pQr = cmd.CreateParameter();
                                pQr.ParameterName = "@qrCode";
                                pQr.Value = (object)qrPayload ?? DBNull.Value;
                                cmd.Parameters.Add(pQr);

                                await cmd.ExecuteNonQueryAsync();
                            }

                            _logger.LogInformation("Inserted {Count} new ticket(s) for showtime {ShowtimeId}",
                                toInsert.Count, showtimeId);
                        }
                        else
                        {
                            _logger.LogInformation(
                                "No new tickets to insert for showtime {ShowtimeId}: tickets already exist for provided seats.",
                                showtimeId);
                        }

                        if (buyerId.HasValue)
                        {
                            var points = (int)Math.Floor(effectiveFinalAmount / 100m);
                            pointsAwarded = points;

                            if (points > 0)
                            {
                                using var cmd = conn.CreateCommand();
                                cmd.Transaction = dbTx;
                                cmd.CommandType = CommandType.Text;
                                cmd.CommandText = @"
                                        UPDATE dbo.Tbl_Users
                                        SET Users_Points = ISNULL(Users_Points, 0) + @points,
                                            Users_UpdatedAt = @now
                                        WHERE Users_ID = @userId
                                    ";
                                var pPoints = cmd.CreateParameter();
                                pPoints.ParameterName = "@points";
                                pPoints.Value = points;
                                cmd.Parameters.Add(pPoints);

                                var pNow2 = cmd.CreateParameter();
                                pNow2.ParameterName = "@now";
                                pNow2.Value = DateTime.Now;
                                cmd.Parameters.Add(pNow2);

                                var pUserId = cmd.CreateParameter();
                                pUserId.ParameterName = "@userId";
                                pUserId.Value = buyerId.Value;
                                cmd.Parameters.Add(pUserId);

                                var updated = await cmd.ExecuteNonQueryAsync();
                                if (updated > 0)
                                {
                                    _logger.LogInformation("Awarded {Points} points to user {UserId}", points,
                                        buyerId.Value);
                                }
                                else
                                {
                                    _logger.LogWarning(
                                        "Failed to award points: user {UserId} not found.", buyerId.Value);
                                }
                            }
                        }

                        await dbTx.CommitAsync();

                        if (couponId.HasValue && couponId.Value > 0 && buyerId.HasValue)
                        {
                            try
                            {
                                var exists = await _context.TblCouponUsers
                                    .AnyAsync(x => x.CouponId == couponId.Value && x.UsersId == buyerId.Value);
                                if (!exists)
                                {
                                    await _context.Database.ExecuteSqlRawAsync(
                                        "INSERT INTO dbo.Tbl_CouponUser (Coupon_ID, Users_ID) VALUES ({0}, {1})",
                                        couponId.Value, buyerId.Value
                                    );
                                    _logger.LogInformation("Coupon {CouponId} marked used by user {UserId}",
                                        couponId.Value, buyerId.Value);
                                }
                            }
                            catch (Exception exCoupon)
                            {
                                _logger.LogError(exCoupon,
                                    "Error recording coupon usage for user {UserId}", buyerId);
                            }
                        }

                        try
                        {
                            if (buyerId.HasValue)
                            {
                                _logger.LogInformation(
                                    "PaymentSuccess: Calling SendTicketsEmailAsync for user {UserId}",
                                    buyerId.Value);

                                await _ticketEmailService.SendTicketsEmailAsync(
                                    buyerId.Value,
                                    showtimeSeatIds,
                                    effectiveOriginalAmount,
                                    effectiveDiscountAmount,
                                    effectiveFinalAmount);
                            }
                            else
                            {
                                _logger.LogWarning(
                                    "PaymentSuccess: buyerId is null, skipping SendTicketsEmailAsync.");
                            }
                        }
                        catch (Exception exEmail)
                        {
                            _logger.LogError(exEmail,
                                "Error sending ticket email after successful commit for user {UserId}", buyerId);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Error while finalizing payment and writing seats/tickets - rolling back.");
                        try { await dbTx.RollbackAsync(); }
                        catch (Exception rbEx)
                        {
                            _logger.LogError(rbEx, "Error while rolling back transaction.");
                        }

                        ViewData["PaymentProcessingError"] = ex.Message;
                        ViewData["SeatLabels"] = new List<string>();
                        ViewData["ShowtimeId"] = showtimeId;
                        ViewData["OriginalAmount"] = 0m;
                        ViewData["DiscountAmount"] = 0m;
                        ViewData["TotalAmount"] = 0m;
                        ViewData["PricePerSeat"] = pricePerSeat;
                        ViewData["ShowtimeSeatIds"] = "";
                        ViewData["PointsAwarded"] = 0;
                        return View();
                    }
                }
                // KHÔNG Dispose/Close conn ở đây – DbContext sẽ lo

                var seatLabelList = seatMappingsResult.Select(x => x.SeatLabel).ToList();

                ViewData["SeatLabels"] = seatLabelList;
                ViewData["ShowtimeId"] = showtimeId;
                ViewData["OriginalAmount"] = effectiveOriginalAmount;
                ViewData["DiscountAmount"] = effectiveDiscountAmount;
                ViewData["TotalAmount"] = effectiveFinalAmount;
                ViewData["PricePerSeat"] = pricePerSeat;
                ViewData["ShowtimeSeatIds"] = string.Join(",", showtimeSeatIds);
                ViewData["PointsAwarded"] = pointsAwarded;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in PaymentSuccess");
                ViewData["PaymentProcessingError"] = ex.Message;
                ViewData["SeatLabels"] = new List<string>();
                ViewData["ShowtimeId"] = showtimeId;
                ViewData["OriginalAmount"] = 0m;
                ViewData["DiscountAmount"] = 0m;
                ViewData["TotalAmount"] = 0m;
                ViewData["PricePerSeat"] = 0m;
                ViewData["ShowtimeSeatIds"] = "";
                ViewData["PointsAwarded"] = 0;
                return View();
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendTicketEmailAjax([FromForm] string showtimeSeatIds)
        {
            if (string.IsNullOrWhiteSpace(showtimeSeatIds))
                return BadRequest(new { success = false, message = "No seat ids provided." });

            var ids = showtimeSeatIds
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => int.TryParse(s.Trim(), out var v) ? v : 0)
                .Where(v => v > 0)
                .Distinct()
                .ToList();

            if (!ids.Any())
                return BadRequest(new { success = false, message = "Invalid seat ids." });

            var claim = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(claim) || !int.TryParse(claim, out var userId))
                return Unauthorized(new { success = false, message = "User not authenticated." });

            try
            {
                await _ticketEmailService.SendTicketsEmailAsync(userId, ids);
                return Ok(new { success = true, message = "Email sent (or queued) to user." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending ticket email via AJAX for user {UserId}", userId);
                return StatusCode(500, new { success = false, message = "Server error sending email." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ValidateCouponAjax(
            [FromForm] int couponId,
            [FromForm] int showtimeId,
            [FromForm] string seatIds,
            [FromForm] decimal amount)
        {
            try
            {
                if (User?.Identity?.IsAuthenticated != true)
                    return Unauthorized(new { success = false, message = "Vui lòng đăng nhập để sử dụng mã giảm giá." });

                var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(claim, out var userId))
                    return Unauthorized(new { success = false, message = "Không xác thực được người dùng." });

                var coupon = _context.TblCoupons
                    .FirstOrDefault(c => c.CouponId == couponId && (c.CouponIsActive ?? false));
                if (coupon == null)
                    return BadRequest(new { success = false, message = "Mã không tồn tại hoặc không hoạt động." });

                var now = DateTime.Now;
                if (coupon.CouponValidFrom > now || coupon.CouponValidTo < now)
                    return BadRequest(new { success = false, message = "Mã chưa có hiệu lực hoặc đã hết hạn." });

                var user = _context.TblUsers.FirstOrDefault(u => u.UsersId == userId);
                if (user == null)
                    return Unauthorized(new { success = false, message = "User not found." });

                if (coupon.CouponMinimumPointsRequired.HasValue &&
                    (user.UsersPoints ?? 0) < coupon.CouponMinimumPointsRequired.Value)
                    return BadRequest(new
                    {
                        success = false,
                        message =
                            $"You don't have enough points to use this code. Need {coupon.CouponMinimumPointsRequired} points."
                    });

                var used = _context.TblCouponUsers.Any(x => x.CouponId == couponId && x.UsersId == userId);
                if (used)
                    return BadRequest(new { success = false, message = "You have already used this code." });

                decimal disc = Math.Floor(amount * (coupon.CouponDiscountPercent / 100m));
                decimal newTotal = Math.Max(0m, amount - disc);

                return Ok(new { success = true, discountAmount = disc, newTotal = newTotal });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ValidateCouponAjax error");
                return StatusCode(500, new { success = false, message = "Lỗi server khi kiểm tra mã giảm giá." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> PaymentFailed(int showtimeId, string seatIds, string message = "")
        {
            try
            {
                var seatIdList = (seatIds ?? "")
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => int.TryParse(s.Trim(), out var v) ? v : 0)
                    .Where(v => v > 0)
                    .ToList();

                if (seatIdList.Any())
                {
                    var seats = await _context.TblShowtimeSeats
                        .Where(s => seatIdList.Contains(s.ShowtimeSeatId))
                        .ToListAsync();

                    foreach (var seat in seats)
                    {
                        seat.ShowtimeSeatStatus = "available";
                        seat.ShowtimeSeatReservedByUserId = default;
                        seat.ShowtimeSeatReservedAt = default(DateTime);
                        seat.ShowtimeSeatUpdatedAt = DateTime.Now;
                    }

                    await _context.SaveChangesAsync();
                }

                _logger.LogInformation("Payment failed. Released {Count} seat(s) for showtime {ShowtimeId}",
                    seatIdList.Count, showtimeId);

                ViewData["ErrorMessage"] = string.IsNullOrEmpty(message)
                    ? "Thanh toán thất bại hoặc bị hủy. Ghế đã được giải phóng."
                    : message;
                ViewData["ShowtimeId"] = showtimeId;

                return View("PaymentFailed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while processing payment failure.");
                ViewData["ErrorMessage"] = "Có lỗi xảy ra khi xử lý thanh toán thất bại.";
                return View("PaymentFailed");
            }
        }
    }
}
