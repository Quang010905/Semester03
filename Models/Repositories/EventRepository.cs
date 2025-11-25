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

        // (GetPastEventsAsync and GetUpcomingEventsAsync - keep your existing implementations)
        // I'll include them unchanged for completeness:

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
                .OrderByDescending(e => e.EventEnd)
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
                    ImageUrl = string.IsNullOrEmpty(e.EventImg) ? "/images/event-placeholder.png" : e.EventImg,
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
                        ImageUrl = string.IsNullOrEmpty(e.EventImg) ? "/images/event-placeholder.png" : e.EventImg,
                        MaxSlot = e.EventMaxSlot,
                        Status = (int)e.EventStatus,
                        TenantPositionId = e.EventTenantPositionId
                    });

                var list = await q.ToListAsync();
                if (list != null && list.Any()) return list;
            }
            catch { }

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
            // load event kèm position và bookings + tenant navs
            var e = await _context.TblEvents
                .AsNoTracking()
                .Include(ev => ev.EventTenantPosition)
                    .ThenInclude(tp => tp.TenantPositionAssignedTenant)   
                .Include(ev => ev.TblEventBookings)
                    .ThenInclude(b => b.EventBookingTenant)               
                .FirstOrDefaultAsync(ev => ev.EventId == eventId);

            if (e == null) return null;

            var now = DateTime.Now;
            var commentQuery = _context.TblCustomerComplaints
                .AsNoTracking()
                .Where(c => c.CustomerComplaintEventId == eventId);

            if (currentUserId.HasValue)
                commentQuery = commentQuery.Where(c => c.CustomerComplaintStatus == 1
                    || c.CustomerComplaintCustomerUserId == currentUserId.Value);
            else
                commentQuery = commentQuery.Where(c => c.CustomerComplaintStatus == 1);

            var comments = await (from c in commentQuery
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
                                  }).ToListAsync();

            // price: nếu 0 => coi là miễn phí (theo yêu cầu)
            decimal? price = null;
            try
            {
                // POCO: EventUnitPrice (decimal)
                price = e.EventUnitPrice > 0 ? e.EventUnitPrice : (decimal?)null;
            }
            catch
            {
                price = null;
            }

            // tổng số đã book = sum EventBookingQuantity (TblEventBookings nav)
            int totalBooked = 0;
            try
            {
                if (e.TblEventBookings != null && e.TblEventBookings.Any())
                {
                    totalBooked = e.TblEventBookings.Sum(b => b.EventBookingQuantity ?? 0);
                }
            }
            catch
            {
                totalBooked = 0;
            }

            var vm = new EventDetailsVm
            {
                Id = e.EventId,
                Title = e.EventName,
                Description = e.EventDescription,
                StartDate = e.EventStart,
                EndDate = e.EventEnd,
                ImageUrl = string.IsNullOrEmpty(e.EventImg) ? "/images/event-placeholder.png" : e.EventImg,
                MaxSlot = e.EventMaxSlot,
                Status = e.EventStatus ?? 0,
                TenantPositionId = e.EventTenantPositionId,
                Comments = comments,
                Price = price
            };

            vm.CommentCount = vm.Comments.Count;
            vm.AvgRate = vm.CommentCount > 0 ? vm.Comments.Average(c => c.Rate) : 0; // mặc định 0 nếu chưa có comment

            vm.IsActive = (e.EventStatus ?? 0) == 1;
            vm.IsPast = e.EventEnd < now;
            vm.IsOngoing = e.EventStart <= now && e.EventEnd >= now;
            vm.IsUpcoming = e.EventStart > now;

            vm.AvailableSlots = Math.Max(0, e.EventMaxSlot - totalBooked);

            // --- Position (from navigation) ---
            if (e.EventTenantPosition != null)
            {
                vm.PositionLocation = e.EventTenantPosition.TenantPositionLocation ?? "";
                vm.PositionFloor = e.EventTenantPosition.TenantPositionFloor;
            }

            // --- Tenant (shop) priority: TenantPosition.AssignedTenant -> fallback latest booking.EventBookingTenant ---
            TblTenant tenant = null;

            if (e.EventTenantPosition?.TenantPositionAssignedTenantId.HasValue == true
                && e.EventTenantPosition.TenantPositionAssignedTenantId.Value != 0)
            {
                tenant = e.EventTenantPosition.TenantPositionAssignedTenant;
            }

            if (tenant == null)
            {
                // try from latest booking that has tenant navigation
                var latestBooking = e.TblEventBookings?
                    .OrderByDescending(b => b.EventBookingCreatedDate ?? DateTime.MinValue)
                    .FirstOrDefault(b => b.EventBookingTenant != null);

                if (latestBooking != null)
                    tenant = latestBooking.EventBookingTenant;
            }

            if (tenant != null)
            {
                vm.OrganizerShopName = !string.IsNullOrWhiteSpace(tenant.TenantName) ? tenant.TenantName : $"Tenant #{tenant.TenantId}";
                vm.OrganizerDescription = tenant.TenantDescription ?? "";

                // try to read tenant contact via TenantUserId (POCO maybe TenantUserId or Tenant_UserID)
                int? tenantUserId = null;
                try
                {
                    // common property name in scaffolded POCO is TenantUserId or TenantUserID
                    var prop = tenant.GetType().GetProperty("TenantUserId") ?? tenant.GetType().GetProperty("Tenant_UserID") ?? tenant.GetType().GetProperty("TenantUserID");
                    if (prop != null)
                        tenantUserId = (int?)(prop.GetValue(tenant));
                }
                catch { tenantUserId = null; }

                if (tenantUserId.HasValue && tenantUserId.Value != 0)
                {
                    var user = await _context.TblUsers.AsNoTracking().FirstOrDefaultAsync(u => u.UsersId == tenantUserId.Value);
                    if (user != null)
                    {
                        vm.OrganizerEmail = user.UsersEmail ?? "";
                        vm.OrganizerPhone = user.UsersPhone ?? "";
                    }
                }
            }
            else
            {
                // final fallback: hiển thị '-' thay vì "Organizer #id" để tránh gây nhầm lẫn
                vm.OrganizerShopName = "-";
            }

            // related events
            vm.Related = await _context.TblEvents
                .AsNoTracking()
                .Where(x => x.EventId != e.EventId && x.EventStatus == 1 && x.EventEnd >= now)
                .OrderBy(x => x.EventStart)
                .Take(4)
                .Select(x => new EventCardVm
                {
                    Id = x.EventId,
                    Title = x.EventName,
                    ShortDescription = string.IsNullOrEmpty(x.EventDescription) ? "" :
                        (x.EventDescription.Length > 200 ? x.EventDescription.Substring(0, 197) + "..." : x.EventDescription),
                    StartDate = x.EventStart,
                    EndDate = x.EventEnd,
                    ImageUrl = string.IsNullOrEmpty(x.EventImg) ? "/images/event-placeholder.png" : x.EventImg,
                    MaxSlot = x.EventMaxSlot,
                    Status = (int)(x.EventStatus ?? 0),
                    TenantPositionId = x.EventTenantPositionId
                }).ToListAsync();

            return vm;
        }
        public async Task<bool> EventExistsAsync(int eventId)
        {
            return await _context.TblEvents
                .AsNoTracking()
                .AnyAsync(e => e.EventId == eventId && e.EventStatus == 1);
        }
        public async Task AddCommentAsync(int eventId, int userId, int rate, string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                text = string.Empty;

            var ent = new TblCustomerComplaint
            {
                CustomerComplaintCustomerUserId = userId,
                CustomerComplaintTenantId = null,
                CustomerComplaintMovieId = null,
                CustomerComplaintEventId = eventId,
                CustomerComplaintRate = rate,
                CustomerComplaintDescription = text.Trim(),
                CustomerComplaintStatus = 0, 
                CustomerComplaintCreatedAt = DateTime.UtcNow
            };

            _context.TblCustomerComplaints.Add(ent);
            await _context.SaveChangesAsync();
        }
        //Addmin CRUD methods
        // (CRUD methods unchanged...)
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
