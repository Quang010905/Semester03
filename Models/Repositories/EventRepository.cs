using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Semester03.Areas.Client.Models.ViewModels;
using Semester03.Models.Entities;

namespace Semester03.Areas.Client.Repositories
{
    /// <summary>
    /// Singleton EventRepository.
    /// NOTE: This repository creates a new AbcdmallContext per-call to avoid DbContext thread-safety issues.
    /// </summary>
    public sealed class EventRepository
    {
        private static readonly Lazy<EventRepository> _lazy =
            new Lazy<EventRepository>(() => new EventRepository());

        public static EventRepository Instance => _lazy.Value;

        // Private ctor to enforce singleton
        private EventRepository()
        {
        }

        /// <summary>
        /// Get featured events (future events) limited to top N.
        /// Uses the columns from Tbl_Event: Event_ID, Event_Name, Event_Description, Event_Start, Event_End, Event_Img, Event_MaxSlot, Event_Status, Event_TenantPositionID
        /// </summary>
        public async Task<List<EventCardVm>> GetFeaturedEventsAsync(int top = 3)
        {
            var now = DateTime.Now;

            using var db = new AbcdmallContext();
            var q = db.TblEvents
                .AsNoTracking()
                .Where(e => e.EventStart >= now && (e.EventStatus == null || e.EventStatus == 1))
                .OrderBy(e => e.EventStart)
                .Take(top)
                .Select(e => new EventCardVm
                {
                    Id = e.EventId,
                    Title = e.EventName,
                    ShortDescription = (e.EventDescription ?? "").Length > 200 ? ((string)e.EventDescription).Substring(0, 197) + "..." : (e.EventDescription ?? ""),
                    StartDate = e.EventStart,
                    EndDate = e.EventEnd,
                    ImageUrl = string.IsNullOrEmpty(e.EventImg) ? "/images/event-placeholder.png" : e.EventImg,
                    MaxSlot = e.EventMaxSlot,
                    Status = e.EventStatus ?? 1,
                    TenantPositionId = e.EventTenantPositionId
                });

            return await q.ToListAsync();
        }

        /// <summary>
        /// Get all upcoming events (events whose end or start is in the future) ordered by start date.
        /// </summary>
        public async Task<List<EventCardVm>> GetUpcomingEventsAsync()
        {
            var now = DateTime.Now;

            using var db = new AbcdmallContext();
            var q = db.TblEvents
                .AsNoTracking()
                .Where(e => ((e.EventEnd == null && e.EventStart >= now) || (e.EventEnd != null && e.EventEnd >= now))
                            && (e.EventStatus == null || e.EventStatus == 1))
                .OrderBy(e => e.EventStart)
                .Select(e => new EventCardVm
                {
                    Id = e.EventId,
                    Title = e.EventName,
                    ShortDescription = (e.EventDescription ?? "").Length > 200 ? ((string)e.EventDescription).Substring(0, 197) + "..." : (e.EventDescription ?? ""),
                    StartDate = e.EventStart,
                    EndDate = e.EventEnd,
                    ImageUrl = string.IsNullOrEmpty(e.EventImg) ? "/images/event-placeholder.png" : e.EventImg,
                    MaxSlot = e.EventMaxSlot,
                    Status = e.EventStatus ?? 1,
                    TenantPositionId = e.EventTenantPositionId
                });

            return await q.ToListAsync();
        }

        /// <summary>
        /// Get full event details by id
        /// </summary>
        public async Task<EventDetailsVm> GetEventByIdAsync(int eventId)
        {
            using var db = new AbcdmallContext();
            var e = await db.TblEvents
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
