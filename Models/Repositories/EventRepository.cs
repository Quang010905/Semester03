using Semester03.Areas.Client.Models.ViewModels;
using Semester03.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Semester03.Models.Repositories
{
    public class EventRepository
    {
        private readonly AbcdmallContext _context;

        public EventRepository(AbcdmallContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // ==========================
        // CLIENT: FEATURED EVENTS
        // ==========================
        public async Task<List<EventCardVm>> GetFeaturedEventsAsync(int top = 3)
        {
            var now = DateTime.Now;

            var q =
                from e in _context.TblEvents.AsNoTracking()
                join tp in _context.TblTenantPositions.AsNoTracking()
                    on e.EventTenantPositionId equals tp.TenantPositionId into tpJoin
                from tp in tpJoin.DefaultIfEmpty()
                join t in _context.TblTenants.AsNoTracking()
                    on tp.TenantPositionAssignedTenantId equals t.TenantId into tJoin
                from t in tJoin.DefaultIfEmpty()
                where e.EventStart >= now && e.EventStatus == 1
                orderby e.EventStart
                select new EventCardVm
                {
                    Id = e.EventId,
                    Title = e.EventName,
                    ShortDescription = string.IsNullOrEmpty(e.EventDescription)
                        ? ""
                        : (e.EventDescription.Length > 200 ? e.EventDescription.Substring(0, 197) + "..." : e.EventDescription),
                    StartDate = e.EventStart,
                    EndDate = e.EventEnd,
                    ImageUrl = string.IsNullOrEmpty(e.EventImg) ? "/images/event-placeholder.png" : e.EventImg,
                    MaxSlot = e.EventMaxSlot,
                    Status = (int)e.EventStatus,
                    TenantPositionId = e.EventTenantPositionId,
                    TenantName = t != null ? t.TenantName : null
                };

            return await q.Take(top).ToListAsync();
        }

        // ==========================
        // CLIENT: UPCOMING EVENTS
        // ==========================
        public async Task<List<EventCardVm>> GetUpcomingEventsAsync(int top = 6)
        {
            var now = DateTime.Now;

            try
            {
                var q =
                    from e in _context.TblEvents.AsNoTracking()
                    join tp in _context.TblTenantPositions.AsNoTracking()
                        on e.EventTenantPositionId equals tp.TenantPositionId into tpJoin
                    from tp in tpJoin.DefaultIfEmpty()
                    join t in _context.TblTenants.AsNoTracking()
                        on tp.TenantPositionAssignedTenantId equals t.TenantId into tJoin
                    from t in tJoin.DefaultIfEmpty()
                    where e.EventEnd >= now && e.EventStatus == 1
                    orderby e.EventStart
                    select new EventCardVm
                    {
                        Id = e.EventId,
                        Title = e.EventName,
                        ShortDescription = string.IsNullOrEmpty(e.EventDescription)
                            ? ""
                            : (e.EventDescription.Length > 200
                                ? e.EventDescription.Substring(0, 197) + "..."
                                : e.EventDescription),
                        StartDate = e.EventStart,
                        EndDate = e.EventEnd,
                        ImageUrl = string.IsNullOrEmpty(e.EventImg) ? "/images/event-placeholder.png" : e.EventImg,
                        MaxSlot = e.EventMaxSlot,
                        Status = (int)e.EventStatus,
                        TenantPositionId = e.EventTenantPositionId,
                        TenantName = t != null ? t.TenantName : null
                    };

                var list = await q.Take(top).ToListAsync();
                if (list != null && list.Any())
                    return list;
            }
            catch
            {
                // fallback bên dưới
            }

            var defaults = new List<EventCardVm>(top);
            for (int i = 1; i <= top; i++)
            {
                defaults.Add(new EventCardVm
                {
                    Id = 0,
                    Title = $"Sự kiện sắp tới #{i}",
                    ShortDescription = "Thông tin sự kiện sẽ được cập nhật sớm nhất.",
                    StartDate = now.AddDays(i),
                    EndDate = now.AddDays(i).AddHours(2),
                    ImageUrl = "/images/event-placeholder.png",
                    MaxSlot = 0,
                    Status = 0,
                    TenantPositionId = 0,
                    TenantName = null
                });
            }

            return defaults;
        }

        // ==========================
        // CLIENT: EVENT DETAILS
        // ==========================
        public async Task<EventDetailsVm> GetEventByIdAsync(int eventId)
        {
            var row =
                await (from ev in _context.TblEvents.AsNoTracking()
                       join tp in _context.TblTenantPositions.AsNoTracking()
                           on ev.EventTenantPositionId equals tp.TenantPositionId into tpJoin
                       from tp in tpJoin.DefaultIfEmpty()
                       join t in _context.TblTenants.AsNoTracking()
                           on tp.TenantPositionAssignedTenantId equals t.TenantId into tJoin
                       from t in tJoin.DefaultIfEmpty()
                       where ev.EventId == eventId
                       select new
                       {
                           Event = ev,
                           TenantName = t != null ? t.TenantName : null
                       }).FirstOrDefaultAsync();

            if (row == null) return null;

            var e = row.Event;

            var vm = new EventDetailsVm
            {
                Id = e.EventId,
                Title = e.EventName,
                Description = e.EventDescription,
                StartDate = e.EventStart,
                EndDate = e.EventEnd,
                ImageUrl = string.IsNullOrEmpty(e.EventImg) ? "/images/event-placeholder.png" : e.EventImg,
                MaxSlot = e.EventMaxSlot,
                Status = (int)e.EventStatus,
                TenantPositionId = e.EventTenantPositionId,
                TenantName = row.TenantName
            };

            try
            {
                if (e.EventTenantPositionId != 0)
                {
                    vm.Related = await GetRelatedEventsAsync(e.EventTenantPositionId, e.EventId, 8);
                }
                else
                {
                    vm.Related = new List<EventCardVm>();
                }
            }
            catch
            {
                vm.Related = new List<EventCardVm>();
            }

            return vm;
        }

        /// <summary>
        /// Lấy các sự kiện liên quan (cùng TenantPosition). Loại bỏ event hiện tại.
        /// </summary>
        public async Task<List<EventCardVm>> GetRelatedEventsAsync(int tenantPositionId, int excludeId, int take = 4)
        {
            var now = DateTime.Now;

            var q =
                from e in _context.TblEvents.AsNoTracking()
                join tp in _context.TblTenantPositions.AsNoTracking()
                    on e.EventTenantPositionId equals tp.TenantPositionId into tpJoin
                from tp in tpJoin.DefaultIfEmpty()
                join t in _context.TblTenants.AsNoTracking()
                    on tp.TenantPositionAssignedTenantId equals t.TenantId into tJoin
                from t in tJoin.DefaultIfEmpty()
                where e.EventTenantPositionId == tenantPositionId
                      && e.EventId != excludeId
                      && e.EventStatus == 1
                      && e.EventEnd >= now
                orderby e.EventStart
                select new EventCardVm
                {
                    Id = e.EventId,
                    Title = e.EventName,
                    ShortDescription = string.IsNullOrEmpty(e.EventDescription)
                        ? ""
                        : (e.EventDescription.Length > 120
                            ? e.EventDescription.Substring(0, 117) + "..."
                            : e.EventDescription),
                    StartDate = e.EventStart,
                    EndDate = e.EventEnd,
                    ImageUrl = string.IsNullOrEmpty(e.EventImg) ? "/images/event-placeholder.png" : e.EventImg,
                    MaxSlot = e.EventMaxSlot,
                    Status = (int)e.EventStatus,
                    TenantPositionId = e.EventTenantPositionId,
                    TenantName = t != null ? t.TenantName : null
                };

            var list = await q.Take(take).ToListAsync();

            if (list == null || !list.Any())
            {
                var fallback =
                    await (from e in _context.TblEvents.AsNoTracking()
                           join tp in _context.TblTenantPositions.AsNoTracking()
                               on e.EventTenantPositionId equals tp.TenantPositionId into tpJoin
                           from tp in tpJoin.DefaultIfEmpty()
                           join t in _context.TblTenants.AsNoTracking()
                               on tp.TenantPositionAssignedTenantId equals t.TenantId into tJoin
                           from t in tJoin.DefaultIfEmpty()
                           where e.EventId != excludeId
                                 && e.EventStatus == 1
                                 && e.EventEnd >= now
                           orderby e.EventStart
                           select new EventCardVm
                           {
                               Id = e.EventId,
                               Title = e.EventName,
                               ShortDescription = string.IsNullOrEmpty(e.EventDescription)
                                   ? ""
                                   : (e.EventDescription.Length > 120
                                       ? e.EventDescription.Substring(0, 117) + "..."
                                       : e.EventDescription),
                               StartDate = e.EventStart,
                               EndDate = e.EventEnd,
                               ImageUrl = string.IsNullOrEmpty(e.EventImg)
                                   ? "/images/event-placeholder.png"
                                   : e.EventImg,
                               MaxSlot = e.EventMaxSlot,
                               Status = (int)e.EventStatus,
                               TenantPositionId = e.EventTenantPositionId,
                               TenantName = t != null ? t.TenantName : null
                           }).Take(take).ToListAsync();

                return fallback ?? new List<EventCardVm>();
            }

            return list;
        }

        // ==========================================================
        // ADMIN CRUD METHODS (GIỮ NGUYÊN)
        // ==========================================================

        public async Task<IEnumerable<TblEvent>> GetAllAsync()
        {
            return await _context.TblEvents
                .Include(e => e.EventTenantPosition)
                .OrderByDescending(e => e.EventStart)
                .ToListAsync();
        }

        public async Task<TblEvent> GetByIdAdminAsync(int id)
        {
            return await _context.TblEvents
                .Include(e => e.EventTenantPosition)
                .Include(e => e.TblEventBookings)
                .FirstOrDefaultAsync(e => e.EventId == id);
        }

        public async Task<TblEvent> AddAsync(TblEvent entity)
        {
            _context.TblEvents.Add(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task UpdateAsync(TblEvent entity)
        {
            _context.Entry(entity).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _context.TblEvents.FindAsync(id);
            if (entity != null)
            {
                _context.TblEvents.Remove(entity);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> UpdateStatusAsync(int eventId, int status)
        {
            var evt = await _context.TblEvents.FindAsync(eventId);
            if (evt == null)
            {
                return false;
            }

            evt.EventStatus = status;
            _context.TblEvents.Update(evt);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
