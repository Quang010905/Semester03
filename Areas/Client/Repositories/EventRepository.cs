using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Semester03.Areas.Client.Models.ViewModels;
using Semester03.Models.Entities; // adjust if your DbContext namespace different

namespace Semester03.Areas.Client.Repositories
{
    public class EventRepository : IEventRepository
    {
        private readonly AbcdmallContext _db;
        public EventRepository(AbcdmallContext db)
        {
            _db = db;
        }

        // NOTE: If your scaffolded DbSet is named differently (Tbl_Event, TblEvents, etc.)
        // adjust _db.TblEvents to the correct property name.

        public async Task<List<EventCardVm>> GetFeaturedEventsAsync(int top = 3)
        {
            var now = DateTime.Now;
            var q = _db.TblEvents
                .AsNoTracking()
                .Where(e => e.EventStart >= now && (e.EventStatus == null || e.EventStatus == 1))
                .OrderBy(e => e.EventStart)
                .Take(top)
                .Select(e => new EventCardVm
                {
                    Id = e.EventId,
                    Title = e.EventName,
                    ShortDescription = e.EventDescription ?? "",
                    StartDate = e.EventStart,
                    EndDate = e.EventEnd,
                    ImageUrl = string.IsNullOrEmpty(e.EventImg) ? "/images/event-placeholder.png" : e.EventImg,
                    MaxSlot = e.EventMaxSlot,
                    Status = e.EventStatus ?? 1
                });

            return await q.ToListAsync();
        }

        public async Task<List<EventCardVm>> GetUpcomingEventsAsync()
        {
            var now = DateTime.Now;
            var q = _db.TblEvents
                .AsNoTracking()
                .Where(e => (e.EventEnd == null ? e.EventStart >= now : e.EventEnd >= now) && (e.EventStatus == null || e.EventStatus == 1))
                .OrderBy(e => e.EventStart)
                .Select(e => new EventCardVm
                {
                    Id = e.EventId,
                    Title = e.EventName,
                    ShortDescription = e.EventDescription ?? "",
                    StartDate = e.EventStart,
                    EndDate = e.EventEnd,
                    ImageUrl = string.IsNullOrEmpty(e.EventImg) ? "/images/event-placeholder.png" : e.EventImg,
                    MaxSlot = e.EventMaxSlot,
                    Status = e.EventStatus ?? 1
                });

            return await q.ToListAsync();
        }

        public async Task<EventDetailsVm> GetEventByIdAsync(int eventId)
        {
            var e = await _db.TblEvents
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.EventId == eventId);

            if (e == null) return null;

            return new EventDetailsVm
            {
                Id = e.EventId,
                Title = e.EventName,
                Description = e.EventDescription ?? "",
                StartDate = e.EventStart,
                EndDate = e.EventEnd,
                ImageUrl = string.IsNullOrEmpty(e.EventImg) ? "/images/event-placeholder.png" : e.EventImg,
                MaxSlot = e.EventMaxSlot,
                Status = e.EventStatus ?? 1,
                TenantPositionId = e.EventTenantPositionId
            };
        }
    }
}
