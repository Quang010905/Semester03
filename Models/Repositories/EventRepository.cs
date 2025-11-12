using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Semester03.Areas.Client.Models.ViewModels;
using Semester03.Models.Entities;

namespace Semester03.Models.Repositories
{
    /// <summary>
    /// EventRepository sử dụng DI (AbcdmallContext được inject).
    /// Lifetime nên là Scoped (mỗi request 1 instance).
    /// </summary>
    public class EventRepository
    {
        private readonly AbcdmallContext _context;

        public EventRepository(AbcdmallContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Get featured events (future events) limited to top N.
        /// </summary>
        public async Task<List<EventCardVm>> GetFeaturedEventsAsync(int top = 3)
        {
            var now = DateTime.Now;

            var q = _context.TblEvents
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
        /// Get all upcoming events ordered by start date.
        /// </summary>
        public async Task<List<EventCardVm>> GetUpcomingEventsAsync()
        {
            var now = DateTime.Now;

            var q = _context.TblEvents
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
        /// Get top N upcoming events (future events) — dùng cho layout.
        /// </summary>
        public async Task<List<EventCardVm>> GetUpcomingEventsAsync(int top)
        {
            // Defensive defaults / validation
            if (top <= 0) top = 6;
            if (_context == null)
                throw new InvalidOperationException("AbcdmallContext is not available. Ensure the repository is constructed by DI.");

            var conn = _context.Database.GetDbConnection();
            if (conn == null || string.IsNullOrWhiteSpace(conn.ConnectionString))
                throw new InvalidOperationException("Database connection string is not configured. Ensure AbcdmallContext is registered with AddDbContext and created by the DI container (do not use 'new AbcdmallContext()').");

            try
            {
                var now = DateTime.Now;

                var q = _context.TblEvents
                    .AsNoTracking()
                    .Where(e =>
                        ((e.EventEnd == null && e.EventStart >= now) || (e.EventEnd != null && e.EventEnd >= now))
                        && (e.EventStatus == null || e.EventStatus == 1))
                    .OrderBy(e => e.EventStart)
                    .Take(top)
                    .Select(e => new EventCardVm
                    {
                        Id = e.EventId,
                        Title = e.EventName,
                        ShortDescription = (e.EventDescription ?? "").Length > 200
                            ? ((string)e.EventDescription).Substring(0, 197) + "..."
                            : (e.EventDescription ?? ""),
                        StartDate = e.EventStart,
                        EndDate = e.EventEnd,
                        ImageUrl = string.IsNullOrEmpty(e.EventImg) ? "/images/event-placeholder.png" : e.EventImg,
                        MaxSlot = e.EventMaxSlot,
                        Status = e.EventStatus ?? 1,
                        TenantPositionId = e.EventTenantPositionId
                    });

                return await q.ToListAsync();
            }
            catch (Exception ex)
            {
                // Wrap with clearer message — giữ inner exception để debug
                throw new InvalidOperationException("Error while reading upcoming events from database. See inner exception for details.", ex);
            }
        }


        /// <summary>
        /// Get full event details by id.
        /// </summary>
        public async Task<EventDetailsVm> GetEventByIdAsync(int eventId)
        {
            var e = await _context.TblEvents
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
