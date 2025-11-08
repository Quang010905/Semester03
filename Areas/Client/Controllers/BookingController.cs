using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Semester03.Areas.Client.Models.ViewModels;
using Semester03.Areas.Client.Models.Vnpay;
using Semester03.Areas.Client.Repositories;
using Semester03.Models.Entities;
using Semester03.Services.Vnpay;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Semester03.Areas.Client.Controllers
{
    [Area("Client")]
    [Route("Client/[controller]/[action]")]
    public class BookingController : Controller
    {
        private readonly IShowtimeRepository _showRepo;
        private readonly IMovieRepository _movieRepo;
        private readonly ISeatRepository _seatRepo;
        private readonly AbcdmallContext _context;
        private readonly IVnPayService _vnPayService;

        public BookingController(
            IShowtimeRepository showRepo,
            IMovieRepository movieRepo,
            ISeatRepository seatRepo,
            IVnPayService vnPayService,
            AbcdmallContext context)
        {
            _showRepo = showRepo;
            _movieRepo = movieRepo;
            _seatRepo = seatRepo;
            _vnPayService = vnPayService;
            _context = context;
        }

        // ========== 1️⃣ TRANG CHỌN PHIM ==========
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

        // ========== 2️⃣ LẤY SUẤT CHIẾU THEO NGÀY ==========
        [HttpGet]
        public IActionResult GetShowtimes(int movieId, string date)
        {
            if (!DateTime.TryParse(date, out var dt))
                return BadRequest("Invalid date");

            var list = _showRepo.GetShowtimesForMovieOnDate(movieId, dt);
            return PartialView("_ShowtimeGrid", list);
        }

        // ========== 3️⃣ GIAO DIỆN CHỌN GHẾ ==========
        [HttpGet]
        public IActionResult SelectSeat(int showtimeId)
        {
            var vm = _seatRepo.GetSeatLayoutForShowtime(showtimeId);
            if (vm == null || vm.Seats == null)
                return NotFound();

            return View(vm);
        }

        // ========== 4️⃣ ĐẶT GHẾ (AJAX) ==========
        [HttpPost]
        public IActionResult ReserveSeats([FromBody] ReserveRequestVm req)
        {
            if (req == null || req.ShowtimeSeatIds == null || !req.ShowtimeSeatIds.Any())
                return BadRequest(new { success = false, message = "No seats selected" });

            int? userId = null;
            if (User?.Identity?.IsAuthenticated == true)
            {
                // lấy user id nếu cần
            }

            var (succeeded, failed) = _seatRepo.ReserveSeats(req.ShowtimeId, req.ShowtimeSeatIds, userId);

            // --- FIX: in-memory join để tránh EF sinh tên cột không đúng ---
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

        // ========== 5️⃣ TRANG XÁC NHẬN ==========
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

            // --- FIX: in-memory join (tránh Join trực tiếp EF -> tên cột ảo) ---
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
                SeatPrice = showtime.ShowtimePrice ?? 0,
                SelectedSeats = seatLabels
            };

            return View("ConfirmBooking", vm);
        }

        // ========== 6️⃣ TẠO URL THANH TOÁN VNPAY ==========
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePaymentUrlVnpay([FromForm] int showtimeId, [FromForm] string seatIds, [FromForm] decimal amount)
        {
            try
            {
                Console.WriteLine("=== CreatePaymentUrlVnpay called ===");
                Console.WriteLine($"Timestamp: {DateTime.UtcNow:O}");
                Console.WriteLine($"Incoming showtimeId = {showtimeId}");
                Console.WriteLine($"Incoming seatIds = '{seatIds}'");
                Console.WriteLine($"Incoming amount (raw) = '{amount}'");

                var model = new PaymentInformationModel
                {
                    OrderType = "movie-ticket",
                    Amount = (double)amount,
                    OrderDescription = $"Showtime:{showtimeId};Seats:{seatIds};Amount:{amount}",
                    Name = $"Booking for Showtime {showtimeId}"
                };

                Console.WriteLine("PaymentInformationModel:");
                Console.WriteLine($"  OrderType = {model.OrderType}");
                Console.WriteLine($"  Amount = {model.Amount} (ToStringInvariant = {model.Amount.ToString(CultureInfo.InvariantCulture)})");
                Console.WriteLine($"  OrderDescription = {model.OrderDescription}");
                Console.WriteLine($"  Name = {model.Name}");

                // Tạo URL bằng service hiện tại
                var url = _vnPayService.CreatePaymentUrl(model, HttpContext);
                Console.WriteLine("VnPayService returned URL:");
                Console.WriteLine(url);

                // --- Debug: thực hiện GET server-side tới URL VNPAY để xem HTML trả về (chỉ debug) ---
                using (var http = new System.Net.Http.HttpClient())
                {
                    http.Timeout = TimeSpan.FromSeconds(10);
                    var resp = await http.GetAsync(url);
                    var body = await resp.Content.ReadAsStringAsync();
                    Console.WriteLine("=== VNPAY HTML RESPONSE (truncated 2000 chars) ===");
                    Console.WriteLine(body.Length > 2000 ? body.Substring(0, 2000) + "...(truncated)..." : body);
                    Console.WriteLine($"HTTP Status: {resp.StatusCode}");
                }

                // Trả về JSON (AJAX)
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = true, url });

                return Redirect(url);
            }
            catch (Exception ex)
            {
                Console.WriteLine("CreatePaymentUrlVnpay exception: " + ex);
                return Json(new { success = false, message = "Lỗi tạo URL VNPAY: " + ex.Message });
            }
        }

        // ========== 7️⃣ CALLBACK TỪ VNPAY ==========
        [HttpGet]
        public IActionResult PaymentCallbackVnpay()
        {
            var response = _vnPayService.PaymentExecute(Request.Query);
            if (response == null)
                return Json(new { success = false, message = "Không nhận được phản hồi từ VNPAY" });

            if (!response.Success)
                return Json(new { success = false, message = "Thanh toán thất bại", code = response.VnPayResponseCode });

            // Tách dữ liệu (OrderDescription format: "Showtime:2;Seats:67,77;Amount:200000")
            int showtimeId = 0;
            string seatIds = "";

            var parts = (response.OrderDescription ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries);
            foreach (var p in parts)
            {
                var trimmed = p.Trim();
                if (trimmed.StartsWith("Showtime:", StringComparison.OrdinalIgnoreCase))
                {
                    int.TryParse(trimmed.Substring("Showtime:".Length).Trim(), out showtimeId);
                }
                else if (trimmed.StartsWith("Seats:", StringComparison.OrdinalIgnoreCase))
                {
                    seatIds = trimmed.Substring("Seats:".Length).Trim();
                }
            }

            // Redirect tới trang success (GET) để hiển thị và ghi nhận vé
            return RedirectToAction("PaymentSuccess", new { showtimeId = showtimeId, seatIds = seatIds });
        }

        // ========== 8️⃣ TRANG THANH TOÁN THÀNH CÔNG ==========
        [HttpGet]
        public async Task<IActionResult> PaymentSuccess(int showtimeId, string seatIds)
        {
            Console.WriteLine($"[PaymentSuccess] called. incoming showtimeId={showtimeId}, seatIds='{seatIds}'");

            // Normalize incoming parts
            var rawParts = (seatIds ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();

            if (!rawParts.Any() && showtimeId <= 0)
            {
                Console.WriteLine("[PaymentSuccess] No seat info and no showtimeId -> returning view.");
                ViewData["SeatLabels"] = new List<string>();
                ViewData["ShowtimeId"] = showtimeId;
                ViewData["TotalAmount"] = 0m;
                ViewData["PricePerSeat"] = 0m;
                return View();
            }

            // 1) Try parse rawParts as showtimeSeatIds (ints)
            var parsedIds = rawParts.Select(s => int.TryParse(s, out var v) ? v : 0).Where(v => v > 0).ToList();
            List<int> showtimeSeatIds = new List<int>();

            if (parsedIds.Any())
            {
                showtimeSeatIds = parsedIds;
                Console.WriteLine($"[PaymentSuccess] parsed showtimeSeatIds: {string.Join(",", showtimeSeatIds)}");
            }
            else
            {
                // rawParts likely seat labels -> map to Seat_ID first (Seat_Label)
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
                    Console.WriteLine($"[PaymentSuccess] mapped to showtimeSeatIds: {string.Join(",", showtimeSeatIds)}");
                }
                else
                {
                    Console.WriteLine("[PaymentSuccess] no seatEntities matched provided labels.");
                }
            }

            if (!showtimeSeatIds.Any())
            {
                Console.WriteLine("[PaymentSuccess] no showtimeSeatIds found -> cannot update DB.");
                ViewData["SeatLabels"] = new List<string>();
                ViewData["ShowtimeId"] = showtimeId;
                ViewData["TotalAmount"] = 0m;
                ViewData["PricePerSeat"] = 0m;
                return View();
            }

            // 2) Infer showtimeId if missing
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
                    Console.WriteLine($"[PaymentSuccess] inferred showtimeId = {showtimeId}");
                    if (possibleShowtimeIds.Count > 1)
                        Console.WriteLine("[PaymentSuccess] Warning: multiple showtimeIds inferred - using first.");
                }
            }

            // 3) Get showtime price
            var showtime = await _context.TblShowtimes.FirstOrDefaultAsync(s => s.ShowtimeId == showtimeId);
            decimal pricePerSeat = showtime?.ShowtimePrice ?? 0m;
            Console.WriteLine($"[PaymentSuccess] pricePerSeat = {pricePerSeat}");

            // buyer id (nullable) from user claims, used for ReservedBy and Ticket_BuyerUserID
            int? buyerId = null;
            var claim = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrWhiteSpace(claim) && int.TryParse(claim, out var parsedBuyer)) buyerId = parsedBuyer;

            // Prepare container to hold mappings for view later
            var seatMappingsResult = new List<(int ShowtimeSeatId, string SeatLabel)>();

            // 4) Raw SQL via DbConnection to avoid EF mapping issues
            var conn = _context.Database.GetDbConnection();
            await using (conn)
            {
                await conn.OpenAsync();

                using (var dbTx = await conn.BeginTransactionAsync())
                {
                    try
                    {
                        // Build IN clause (safe because values are ints parsed earlier)
                        string inClause = string.Join(",", showtimeSeatIds);

                        // 4a) Select showtime-seat rows (ShowtimeSeat_ID, ShowtimeSeat_SeatID, ShowtimeSeat_ShowtimeID)
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
                            Console.WriteLine("[PaymentSuccess] raw select returned 0 showtimeSeatRows -> rollback");
                            await dbTx.RollbackAsync();
                            ViewData["SeatLabels"] = new List<string>();
                            ViewData["ShowtimeId"] = showtimeId;
                            ViewData["TotalAmount"] = 0m;
                            ViewData["PricePerSeat"] = pricePerSeat;
                            return View();
                        }

                        // 4b) Select seat labels from Tbl_Seat
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

                        // Compose seatMappings and compute labels/total
                        var seatMappings = showtimeSeatRows
                            .Select(r => new { r.ShowtimeSeatId, Label = seatLabelsById.ContainsKey(r.SeatId) ? seatLabelsById[r.SeatId] : "", r.ShowtimeId })
                            .ToList();

                        var labels = seatMappings.Select(m => m.Label).ToList();
                        var totalAmount = pricePerSeat * labels.Count;
                        Console.WriteLine($"[PaymentSuccess] seat labels: {string.Join(",", labels)} totalAmount={totalAmount}");

                        // Save mapping for later view use
                        seatMappingsResult = seatMappings.Select(m => (m.ShowtimeSeatId, m.Label)).ToList();

                        // 5) Update Tbl_ShowtimeSeat (set status, updatedAt, reservedBy, reservedAt)
                        using (var cmd = conn.CreateCommand())
                        {
                            cmd.Transaction = dbTx;
                            cmd.CommandType = CommandType.Text;
                            cmd.CommandText = $@"
                        UPDATE dbo.Tbl_ShowtimeSeat
                        SET ShowtimeSeat_Status = @status,
                            ShowtimeSeat_UpdatedAt = @now,
                            ShowtimeSeat_ReservedByUserID = @reservedBy,
                            ShowtimeSeat_ReservedAt = @reservedAt
                        WHERE ShowtimeSeat_ID IN ({inClause})
                    ";

                            var pStatus = cmd.CreateParameter(); pStatus.ParameterName = "@status"; pStatus.Value = "sold"; cmd.Parameters.Add(pStatus);
                            var pNow = cmd.CreateParameter(); pNow.ParameterName = "@now"; pNow.Value = DateTime.UtcNow; cmd.Parameters.Add(pNow);
                            var pReservedBy = cmd.CreateParameter(); pReservedBy.ParameterName = "@reservedBy";
                            pReservedBy.Value = (object?)buyerId ?? DBNull.Value; cmd.Parameters.Add(pReservedBy);
                            var pReservedAt = cmd.CreateParameter(); pReservedAt.ParameterName = "@reservedAt"; pReservedAt.Value = DateTime.UtcNow; cmd.Parameters.Add(pReservedAt);

                            var rows = await cmd.ExecuteNonQueryAsync();
                            Console.WriteLine($"[PaymentSuccess] UPDATE Tbl_ShowtimeSeat affected {rows} rows.");
                        }

                        // 6) Insert tickets
                        var ticketsInserted = 0;
                        foreach (var m in seatMappings)
                        {
                            using var cmd = conn.CreateCommand();
                            cmd.Transaction = dbTx;
                            cmd.CommandType = CommandType.Text;
                            cmd.CommandText = @"
                        INSERT INTO dbo.Tbl_Ticket
                            (Ticket_ShowtimeID, Ticket_ShowtimeSeatID, Ticket_Seat, Ticket_BuyerUserID, Ticket_Status, Ticket_Price, Ticket_PurchasedAt)
                        VALUES
                            (@showtimeId, @showtimeSeatId, @seatLabel, @buyerId, @status, @price, @purchasedAt)
                    ";

                            var pShowtimeId = cmd.CreateParameter(); pShowtimeId.ParameterName = "@showtimeId"; pShowtimeId.Value = showtimeId; cmd.Parameters.Add(pShowtimeId);
                            var pShowtimeSeatId = cmd.CreateParameter(); pShowtimeSeatId.ParameterName = "@showtimeSeatId"; pShowtimeSeatId.Value = m.ShowtimeSeatId; cmd.Parameters.Add(pShowtimeSeatId);
                            var pSeatLabel = cmd.CreateParameter(); pSeatLabel.ParameterName = "@seatLabel"; pSeatLabel.Value = (object)m.Label ?? DBNull.Value; cmd.Parameters.Add(pSeatLabel);

                            var pBuyerId = cmd.CreateParameter(); pBuyerId.ParameterName = "@buyerId"; pBuyerId.Value = (object?)buyerId ?? DBNull.Value; cmd.Parameters.Add(pBuyerId);

                            var pStatus2 = cmd.CreateParameter(); pStatus2.ParameterName = "@status"; pStatus2.Value = "sold"; cmd.Parameters.Add(pStatus2);
                            var pPrice = cmd.CreateParameter(); pPrice.ParameterName = "@price"; pPrice.Value = (object)pricePerSeat ?? DBNull.Value; cmd.Parameters.Add(pPrice);
                            var pPurchasedAt = cmd.CreateParameter(); pPurchasedAt.ParameterName = "@purchasedAt"; pPurchasedAt.Value = DateTime.UtcNow; cmd.Parameters.Add(pPurchasedAt);

                            var inserted = await cmd.ExecuteNonQueryAsync();
                            ticketsInserted += inserted;
                        }

                        Console.WriteLine($"[PaymentSuccess] Inserted {ticketsInserted} ticket rows.");

                        // Commit transaction
                        await dbTx.CommitAsync();
                        Console.WriteLine("[PaymentSuccess] DB transaction committed.");
                    }
                    catch (Exception ex)
                    {
                        try { await dbTx.RollbackAsync(); } catch { /* ignore */ }
                        Console.WriteLine("[PaymentSuccess] Exception during raw-sql transaction: " + ex);
                        // fall through to render view (we logged)
                    }
                    finally
                    {
                        await conn.CloseAsync();
                    }
                } // end transaction
            } // end connection

            // Prepare seat labels for view using seatMappingsResult
            var seatLabelList = seatMappingsResult.Select(x => x.SeatLabel).ToList();

            ViewData["SeatLabels"] = seatLabelList;
            ViewData["ShowtimeId"] = showtimeId;
            ViewData["TotalAmount"] = pricePerSeat * seatLabelList.Count;
            ViewData["PricePerSeat"] = pricePerSeat;

            return View();
        }


    }
}
