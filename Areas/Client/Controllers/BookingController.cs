using Microsoft.AspNetCore.Mvc;
using Semester03.Areas.Client.Models.ViewModels;
using Semester03.Areas.Client.Repositories;
using Semester03.Models.Entities;
using Semester03.Services.Vnpay;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Semester03.Areas.Client.Controllers
{
    [Area("Client")]
    [Route("Client/[controller]/[action]")]
    public class BookingController : Controller
    {
        private readonly ShowtimeRepository _showRepo;
        private readonly MovieRepository _movieRepo;
        private readonly SeatRepository _seatRepo;
        private readonly AbcdmallContext _context;
        private readonly IVnPayService _vnPayService;

        public BookingController(
            ShowtimeRepository showRepo,
            MovieRepository movieRepo,
            SeatRepository seatRepo,
            IVnPayService vnPayService,
            AbcdmallContext context)
        {
            _showRepo = showRepo;
            _movieRepo = movieRepo;
            _seatRepo = seatRepo;
            _vnPayService = vnPayService;
            _context = context;
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
                // optionally parse user id claim
                var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(claim, out var id)) userId = id;
            }

            var (succeeded, failed) = _seatRepo.ReserveSeats(req.ShowtimeId, req.ShowtimeSeatIds, userId);

            // in-memory join to get labels
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
                TotalAmount = (showtime.ShowtimePrice) * seatLabels.Count
            };

            return View("ConfirmBooking", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePaymentUrlVnpay([FromForm] int showtimeId, [FromForm] string seatIds, [FromForm] decimal amount)
        {
            try
            {
                var model = new Semester03.Areas.Client.Models.Vnpay.PaymentInformationModel
                {
                    OrderType = "movie-ticket",
                    Amount = (double)amount,
                    OrderDescription = $"Showtime:{showtimeId};Seats:{seatIds};Amount:{amount}",
                    Name = $"Booking for Showtime {showtimeId}"
                };

                var url = _vnPayService.CreatePaymentUrl(model, HttpContext);

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { success = true, url });

                return Redirect(url);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi tạo URL VNPAY: " + ex.Message });
            }
        }

        [HttpGet]
        public IActionResult PaymentCallbackVnpay()
        {
            var response = _vnPayService.PaymentExecute(Request.Query);
            if (response == null)
                return Json(new { success = false, message = "Không nhận được phản hồi từ VNPAY" });

            if (!response.Success)
                return Json(new { success = false, message = "Thanh toán thất bại", code = response.VnPayResponseCode });

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

            return RedirectToAction("PaymentSuccess", new { showtimeId = showtimeId, seatIds = seatIds });
        }

        [HttpGet]
        public async Task<IActionResult> PaymentSuccess(int showtimeId, string seatIds)
        {
            var rawParts = (seatIds ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();

            var parsedIds = rawParts.Select(s => int.TryParse(s, out var v) ? v : 0).Where(v => v > 0).ToList();
            List<int> showtimeSeatIds = new List<int>();

            if (parsedIds.Any())
            {
                showtimeSeatIds = parsedIds;
            }
            else
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
                ViewData["TotalAmount"] = 0m;
                ViewData["PricePerSeat"] = 0m;
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

            var showtime = await _context.TblShowtimes.FirstOrDefaultAsync(s => s.ShowtimeId == showtimeId);
            decimal pricePerSeat = showtime?.ShowtimePrice ?? 0m;

            int? buyerId = null;
            var claim = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrWhiteSpace(claim) && int.TryParse(claim, out var parsedBuyer)) buyerId = parsedBuyer;

            var seatMappingsResult = new List<(int ShowtimeSeatId, string SeatLabel)>();

            var conn = _context.Database.GetDbConnection();
            await using (conn)
            {
                await conn.OpenAsync();

                using (var dbTx = await conn.BeginTransactionAsync())
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
                            ViewData["TotalAmount"] = 0m;
                            ViewData["PricePerSeat"] = pricePerSeat;
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
                            .Select(r => new { r.ShowtimeSeatId, Label = seatLabelsById.ContainsKey(r.SeatId) ? seatLabelsById[r.SeatId] : "", r.ShowtimeId })
                            .ToList();

                        var labels = seatMappings.Select(m => m.Label).ToList();
                        var totalAmount = pricePerSeat * labels.Count;

                        seatMappingsResult = seatMappings.Select(m => (m.ShowtimeSeatId, m.Label)).ToList();

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
                        }

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

                        await dbTx.CommitAsync();
                    }
                    catch (Exception ex)
                    {
                        try { await dbTx.RollbackAsync(); } catch { }
                    }
                    finally
                    {
                        await conn.CloseAsync();
                    }
                }
            }

            var seatLabelList = seatMappingsResult.Select(x => x.SeatLabel).ToList();

            ViewData["SeatLabels"] = seatLabelList;
            ViewData["ShowtimeId"] = showtimeId;
            ViewData["TotalAmount"] = pricePerSeat * seatLabelList.Count;
            ViewData["PricePerSeat"] = pricePerSeat;

            return View();
        }
    }
}
