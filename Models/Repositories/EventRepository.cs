using Semester03.Areas.Client.Models.ViewModels;
using Semester03.Models.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Semester03.Models.Repositories
{
    public class EventRepository
    {
        private readonly AbcdmallContext _context;

        public EventRepository(AbcdmallContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // ===============================
        // SỰ KIỆN NỔI BẬT (ĐÃ KẾT THÚC)
        // Sắp theo: AvgRate ↓, CommentCount ↓, EndDate ↓
        // ===============================
        public async Task<List<EventCardVm>> GetFeaturedEventsAsync(int top = 10)
        {
            var now = DateTime.Now;

            var q = _context.TblEvents
                .AsNoTracking()
                .Where(e => e.EventStatus == 1 && e.EventEnd < now) // ĐÃ KẾT THÚC
                .Select(e => new
                {
                    Event = e,

                    AvgRate = _context.TblCustomerComplaints
                        .Where(c => c.CustomerComplaintEventId == e.EventId
                                    && c.CustomerComplaintStatus == 1)
                        .Average(c => (double?)c.CustomerComplaintRate) ?? 0,

                    CommentCount = _context.TblCustomerComplaints
                        .Count(c => c.CustomerComplaintEventId == e.EventId
                                    && c.CustomerComplaintStatus == 1)
                })
                .OrderByDescending(x => x.AvgRate)
                .ThenByDescending(x => x.CommentCount)
                .ThenByDescending(x => x.Event.EventEnd)
                .Take(top)
                .Select(x => new EventCardVm
                {
                    Id = x.Event.EventId,
                    Title = x.Event.EventName,
                    ShortDescription = x.Event.EventDescription,
                    StartDate = x.Event.EventStart,
                    EndDate = x.Event.EventEnd,
                    ImageUrl = string.IsNullOrEmpty(x.Event.EventImg)
                        ? "/images/event-placeholder.png"
                        : x.Event.EventImg,
                    MaxSlot = x.Event.EventMaxSlot,
                    Status = (int)x.Event.EventStatus,
                    TenantPositionId = x.Event.EventTenantPositionId
                });

            return await q.ToListAsync();
        }


        // ===============================
        // SỰ KIỆN SẮP / ĐANG DIỄN RA
        // EventEnd >= NOW
        // ===============================
        public async Task<List<EventCardVm>> GetUpcomingEventsAsync(int top = 6)
        {
            var now = DateTime.Now;

            try
            {
                var q = _context.TblEvents
                    .AsNoTracking()
                    .Where(e => e.EventEnd >= now && e.EventStatus == 1)
                    .OrderBy(e => e.EventStart)
                    .Take(top)
                    .Select(e => new EventCardVm
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
                        TenantPositionId = e.EventTenantPositionId
                    });

                var list = await q.ToListAsync();

                if (list != null && list.Any())
                    return list;
            }
            catch (Exception)
            {
                // swallow intentionally; return defaults below
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
                    TenantPositionId = 0
                });
            }

            return defaults;
        }


        // ===============================
        // EVENT DETAILS + COMMENT
        // ===============================
        public async Task<EventDetailsVm> GetEventByIdAsync(int eventId, int? currentUserId = null)
        {
            var e = await _context.TblEvents
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.EventId == eventId);

            if (e == null)
                return null;

            var query = _context.TblCustomerComplaints
                .AsNoTracking()
                .Where(c => c.CustomerComplaintEventId == eventId);

            if (currentUserId.HasValue)
            {
                query = query.Where(c =>
                    c.CustomerComplaintStatus == 1 ||
                    c.CustomerComplaintCustomerUserId == currentUserId.Value);
            }
            else
            {
                query = query.Where(c => c.CustomerComplaintStatus == 1);
            }

            var comments = await (
                from c in query
                join u in _context.TblUsers.AsNoTracking()
                    on c.CustomerComplaintCustomerUserId equals u.UsersId into gj
                from user in gj.DefaultIfEmpty()
                orderby c.CustomerComplaintCreatedAt descending
                select new CommentVm
                {
                    Id = c.CustomerComplaintId,
                    UserId = c.CustomerComplaintCustomerUserId,
                    UserName = user != null
                        ? (string.IsNullOrWhiteSpace(user.UsersFullName) ? user.UsersUsername : user.UsersFullName)
                        : "Ẩn danh",
                    Rate = c.CustomerComplaintRate,
                    Text = c.CustomerComplaintDescription,
                    CreatedAt = c.CustomerComplaintCreatedAt
                }
            ).ToListAsync();

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
                Comments = comments
            };

            vm.CommentCount = vm.Comments.Count;
            vm.AvgRate = vm.CommentCount > 0 ? vm.Comments.Average(c => c.Rate) : 0;

            vm.Related = new List<EventCardVm>();

            return vm;
        }


        // CRUD Admin
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
            var evt = await _context.TblEvents.FindAsync(id);

            if (evt != null)
            {
                _context.TblEvents.Remove(evt);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> UpdateStatusAsync(int eventId, int status)
        {
            var evt = await _context.TblEvents.FindAsync(eventId);

            if (evt == null) return false;

            evt.EventStatus = status;
            _context.Update(evt);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
