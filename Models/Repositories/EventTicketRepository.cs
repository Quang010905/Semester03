using Microsoft.EntityFrameworkCore;
using Semester03.Areas.Client.Models.ViewModels;
using Semester03.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Semester03.Models.Repositories
{
    public class EventTicketRepository
    {
        private readonly AbcdmallContext _db;

        public EventTicketRepository(AbcdmallContext db)
        {
            _db = db;
        }

        public async Task<List<MyEventTicketVm>> GetTicketsByUserAsync(int userId)
        {
            var data = await (from b in _db.TblEventBookings
                              join e in _db.TblEvents on b.EventBookingEventId equals e.EventId
                              where b.EventBookingUserId == userId
                              select new
                              {
                                  b.EventBookingId,
                                  Qty = b.EventBookingQuantity,
                                  Cost = b.EventBookingTotalCost,
                                  Status = b.EventBookingStatus,
                                  e.EventName,
                                  e.EventImg,
                                  e.EventStart,
                                  e.EventEnd
                              }).ToListAsync();

            var now = DateTime.Now;

            return data.Select(x =>
            {
                string status =
                    x.Status == 0 ? "Đã hủy" :
                    x.EventEnd <= now ? "Đã diễn ra" :
                    "Sắp diễn ra";

                return new MyEventTicketVm
                {
                    BookingId = x.EventBookingId,
                    EventName = x.EventName,
                    EventImage = x.EventImg ?? "",
                    EventStart = x.EventStart,
                    EventEnd = x.EventEnd,
                    Quantity = x.Qty ?? 1,
                    TotalCost = x.Cost ?? 0m,
                    Status = status
                };
            }).ToList();
        }
    }
}
