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
        // SỰ KIỆN ĐÃ DIỄN RA (PAST) – CÓ PHÂN TRANG
        // EventEnd < NOW
        // ===============================
        public async Task<PagedResult<EventCardVm>> GetPastEventsAsync(int pageIndex, int pageSize)
        {
            var now = DateTime.Now;

            if (pageIndex < 1) pageIndex = 1;
            if (pageSize < 1) pageSize = 9;

            var baseQuery = _context.TblEvents
                .AsNoTracking()
                .Where(e => e.EventStatus == 1 && e.EventEnd < now);

            var totalItems = await baseQuery.CountAsync();

            var items = await baseQuery
                .OrderByDescending(e => e.EventEnd) // Mới kết thúc trước
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .Select(e => new EventCardVm
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
                    ImageUrl = string.IsNullOrEmpty(e.EventImg)
                        ? "/images/event-placeholder.png"
                        : e.EventImg,
                    MaxSlot = e.EventMaxSlot,
                    Status = (int)e.EventStatus,
                    TenantPositionId = e.EventTenantPositionId
                })
                .ToListAsync();

            return new PagedResult<EventCardVm>
            {
                Items = items,
                PageIndex = pageIndex,
                PageSize = pageSize,
                TotalItems = totalItems
            };
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
                            : (e.EventDescription.Length > 200
                                ? e.EventDescription.Substring(0, 197) + "..."
                                : e.EventDescription),
                        StartDate = e.EventStart,
                        EndDate = e.EventEnd,
                        ImageUrl = string.IsNullOrEmpty(e.EventImg)
                            ? "/images/event-placeholder.png"
                            : e.EventImg,
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

            // fallback nếu DB có vấn đề
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

            var now = DateTime.Now;

            var query = _context.TblCustomerComplaints
                .AsNoTracking()
                .Where(c => c.CustomerComplaintEventId == eventId);

            if (currentUserId.HasValue)
            {
                // User hiện tại thấy:
                // - comment đã duyệt
                // - comment của chính mình (kể cả chưa duyệt)
                query = query.Where(c =>
                    c.CustomerComplaintStatus == 1 ||
                    c.CustomerComplaintCustomerUserId == currentUserId.Value);
            }
            else
            {
                // Khách vãng lai chỉ thấy comment đã duyệt
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
            // 👉 Khi chưa có ai bình luận, mặc định 5 sao
            vm.AvgRate = vm.CommentCount > 0 ? vm.Comments.Average(c => c.Rate) : 5;

            // Flag trạng thái
            vm.IsActive = e.EventStatus == 1;
            vm.IsPast = e.EventEnd < now;
            vm.IsOngoing = e.EventStart <= now && e.EventEnd >= now;
            vm.IsUpcoming = e.EventStart > now;

            // Sự kiện liên quan: các event active, chưa kết thúc, khác id
            vm.Related = await _context.TblEvents
                .AsNoTracking()
                .Where(x => x.EventId != e.EventId
                            && x.EventStatus == 1
                            && x.EventEnd >= now)
                .OrderBy(x => x.EventStart)
                .Take(4)
                .Select(x => new EventCardVm
                {
                    Id = x.EventId,
                    Title = x.EventName,
                    ShortDescription = string.IsNullOrEmpty(x.EventDescription)
                        ? ""
                        : (x.EventDescription.Length > 200
                            ? x.EventDescription.Substring(0, 197) + "..."
                            : x.EventDescription),
                    StartDate = x.EventStart,
                    EndDate = x.EventEnd,
                    ImageUrl = string.IsNullOrEmpty(x.EventImg)
                        ? "/images/event-placeholder.png"
                        : x.EventImg,
                    MaxSlot = x.EventMaxSlot,
                    Status = (int)x.EventStatus,
                    TenantPositionId = x.EventTenantPositionId
                })
                .ToListAsync();

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
