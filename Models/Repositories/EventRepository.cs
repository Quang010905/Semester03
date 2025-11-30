using Semester03.Areas.Client.Models.ViewModels;
using Semester03.Models.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Semester03.Areas.Partner.Models;
using System.Globalization;
using System.Text;

namespace Semester03.Models.Repositories
{
    public class EventRepository
    {
        private readonly AbcdmallContext _context;

        public EventRepository(AbcdmallContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // PAST EVENTS (with paging + optional date filter)
        public async Task<PagedResult<EventCardVm>> GetPastEventsAsync(
            int pageIndex,
            int pageSize,
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            var now = DateTime.Now;

            if (pageIndex < 1) pageIndex = 1;
            if (pageSize < 1) pageSize = 9;

            var baseQuery = _context.TblEvents
                .AsNoTracking()
                .Where(e => e.EventStatus == 1 && e.EventEnd < now);

            if (fromDate.HasValue)
            {
                baseQuery = baseQuery.Where(e => e.EventStart >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                baseQuery = baseQuery.Where(e => e.EventStart <= toDate.Value);
            }

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

        // UPCOMING EVENTS (top N, optional date filter)
        public async Task<List<EventCardVm>> GetUpcomingEventsAsync(
            int top = 6,
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            var now = DateTime.Now;

            try
            {
                var query = _context.TblEvents
                    .AsNoTracking()
                    .Where(e => e.EventEnd >= now && e.EventStatus == 1);

                if (fromDate.HasValue)
                {
                    query = query.Where(e => e.EventStart >= fromDate.Value);
                }

                if (toDate.HasValue)
                {
                    query = query.Where(e => e.EventStart <= toDate.Value);
                }

                var list = await query
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
                    })
                    .ToListAsync();

                if (list != null && list.Any())
                    return list;
            }
            catch
            {
            }

            // Default placeholder nếu không có event
            var defaults = new List<EventCardVm>(top);
            for (int i = 1; i <= top; i++)
            {
                defaults.Add(new EventCardVm
                {
                    Id = 0,
                    Title = $"Upcoming event #{i}",
                    ShortDescription = "Event information will be updated soon.",
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

        // ALL UPCOMING EVENTS (for View more)
        public async Task<List<EventCardVm>> GetAllUpcomingEventsAsync(
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            var now = DateTime.Now;

            var q = _context.TblEvents
                .AsNoTracking()
                .Where(e => e.EventEnd >= now && e.EventStatus == 1);

            if (fromDate.HasValue)
            {
                q = q.Where(e => e.EventStart >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                q = q.Where(e => e.EventStart <= toDate.Value);
            }

            return await q.OrderBy(e => e.EventStart)
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
                    Status = (int)(e.EventStatus ?? 0),
                    TenantPositionId = e.EventTenantPositionId
                })
                .ToListAsync();
        }

        // ===============================
        // EVENT DETAILS + COMMENTS (PAGING)
        // ===============================
        public async Task<EventDetailsVm> GetEventByIdAsync(
            int eventId,
            int? currentUserId,
            int commentPage,
            int pageSize)
        {
            if (commentPage < 1) commentPage = 1;
            if (pageSize < 1) pageSize = 5;

            var e = await _context.TblEvents
                .AsNoTracking()
                .Include(ev => ev.EventTenantPosition)
                    .ThenInclude(tp => tp.TenantPositionAssignedTenant)
                .Include(ev => ev.TblEventBookings)
                    .ThenInclude(b => b.EventBookingTenant)
                .FirstOrDefaultAsync(ev => ev.EventId == eventId);

            if (e == null) return null;

            var now = DateTime.Now;

            // ===== COMMENT QUERY BASE =====
            var baseCommentQuery = _context.TblCustomerComplaints
                .AsNoTracking()
                .Where(c => c.CustomerComplaintEventId == eventId);

            if (currentUserId.HasValue)
            {
                baseCommentQuery = baseCommentQuery.Where(c =>
                    c.CustomerComplaintStatus == 1 ||
                    c.CustomerComplaintCustomerUserId == currentUserId.Value);
            }
            else
            {
                baseCommentQuery = baseCommentQuery.Where(c => c.CustomerComplaintStatus == 1);
            }

            var totalComments = await baseCommentQuery.CountAsync();

            double avgRate = 0;
            if (totalComments > 0)
            {
                avgRate = await baseCommentQuery.AverageAsync(c => c.CustomerComplaintRate);
            }

            var comments = await (
                from c in baseCommentQuery
                join u in _context.TblUsers.AsNoTracking()
                    on c.CustomerComplaintCustomerUserId equals u.UsersId into gj
                from user in gj.DefaultIfEmpty()
                orderby c.CustomerComplaintCreatedAt descending
                select new CommentVm
                {
                    Id = c.CustomerComplaintId,
                    UserId = c.CustomerComplaintCustomerUserId,
                    UserName = user != null
                        ? (string.IsNullOrWhiteSpace(user.UsersFullName)
                                ? user.UsersUsername
                                : user.UsersFullName)
                        : "Anonymous",
                    Rate = c.CustomerComplaintRate,
                    Text = c.CustomerComplaintDescription,
                    CreatedAt = c.CustomerComplaintCreatedAt
                })
                .Skip((commentPage - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // ===== PRICE =====
            decimal? price = null;
            try
            {
                price = e.EventUnitPrice > 0 ? e.EventUnitPrice : (decimal?)null;
            }
            catch
            {
                price = null;
            }

            // ===== CONFIRMED SLOTS =====
            int confirmedSlots = 0;
            try
            {
                var confirmedStatuses = new[] { 1, 2 }; // 1 = Paid, 2 = Free / PartiallyRefunded

                var bookings = await _context.TblEventBookings
                    .AsNoTracking()
                    .Where(b => b.EventBookingEventId == e.EventId &&
                                b.EventBookingPaymentStatus != null &&
                                confirmedStatuses.Contains(b.EventBookingPaymentStatus.Value))
                    .Select(b => new { b.EventBookingQuantity, b.EventBookingNotes })
                    .ToListAsync();

                foreach (var b in bookings)
                {
                    int qty = b.EventBookingQuantity ?? 0;

                    if (qty <= 0)
                    {
                        qty = 1;

                        try
                        {
                            var notes = b.EventBookingNotes ?? "";
                            var parts = notes.Split(';', StringSplitOptions.RemoveEmptyEntries);

                            foreach (var part in parts)
                            {
                                var t = part.Trim();
                                if (t.StartsWith("Qty", StringComparison.OrdinalIgnoreCase))
                                {
                                    var digits = new string(t.Where(char.IsDigit).ToArray());
                                    if (int.TryParse(digits, out var q) && q > 0)
                                    {
                                        qty = q;
                                    }
                                    break;
                                }
                            }
                        }
                        catch
                        {
                        }
                    }

                    confirmedSlots += Math.Max(1, qty);
                }
            }
            catch
            {
                confirmedSlots = 0;
            }

            // ===== MAP VM =====
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

            vm.CommentCount = totalComments;
            vm.AvgRate = totalComments > 0 ? avgRate : 0.0;
            vm.CommentPageIndex = commentPage;
            vm.CommentPageSize = pageSize;
            vm.CommentTotalPages = pageSize == 0 ? 0 : (int)Math.Ceiling(totalComments / (double)pageSize);

            vm.IsActive = (e.EventStatus ?? 0) == 1;
            vm.IsPast = e.EventEnd < now;
            vm.IsOngoing = e.EventStart <= now && e.EventEnd >= now;
            vm.IsUpcoming = e.EventStart > now;

            var maxSlot = e.EventMaxSlot;
            vm.AvailableSlots = maxSlot > 0
                ? Math.Max(0, maxSlot - confirmedSlots)
                : 0;

            // --- Position ---
            if (e.EventTenantPosition != null)
            {
                vm.PositionLocation = e.EventTenantPosition.TenantPositionLocation ?? "";
                vm.PositionFloor = e.EventTenantPosition.TenantPositionFloor;
            }

            // --- Tenant (shop) ---
            TblTenant tenant = null;

            if (e.EventTenantPosition?.TenantPositionAssignedTenantId.HasValue == true
                && e.EventTenantPosition.TenantPositionAssignedTenantId.Value != 0)
            {
                tenant = e.EventTenantPosition.TenantPositionAssignedTenant;
            }

            if (tenant == null)
            {
                var latestBooking = e.TblEventBookings?
                    .OrderByDescending(b => b.EventBookingCreatedDate ?? DateTime.MinValue)
                    .FirstOrDefault(b => b.EventBookingTenant != null);

                if (latestBooking != null)
                    tenant = latestBooking.EventBookingTenant;
            }

            if (tenant != null)
            {
                vm.OrganizerShopName = !string.IsNullOrWhiteSpace(tenant.TenantName)
                    ? tenant.TenantName
                    : $"Tenant #{tenant.TenantId}";

                vm.OrganizerDescription = tenant.TenantDescription ?? "";

                int? tenantUserId = null;
                try
                {
                    var prop = tenant.GetType().GetProperty("TenantUserId")
                              ?? tenant.GetType().GetProperty("Tenant_UserID")
                              ?? tenant.GetType().GetProperty("TenantUserID");
                    if (prop != null)
                        tenantUserId = (int?)(prop.GetValue(tenant));
                }
                catch
                {
                    tenantUserId = null;
                }

                if (tenantUserId.HasValue && tenantUserId.Value != 0)
                {
                    var user = await _context.TblUsers.AsNoTracking()
                        .FirstOrDefaultAsync(u => u.UsersId == tenantUserId.Value);

                    if (user != null)
                    {
                        vm.OrganizerEmail = user.UsersEmail ?? "";
                        vm.OrganizerPhone = user.UsersPhone ?? "";
                    }
                }
            }
            else
            {
                vm.OrganizerShopName = null;
                vm.OrganizerDescription = vm.OrganizerDescription ?? "";
            }

            // RELATED EVENTS
            var relatedQuery = _context.TblEvents
                .AsNoTracking()
                .Where(x => x.EventId != e.EventId && x.EventStatus == 1 && x.EventEnd >= now)
                .OrderBy(x => x.EventStart)
                .Take(4);

            vm.Related = await relatedQuery
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
                    ImageUrl = string.IsNullOrEmpty(x.EventImg) ? "/images/event-placeholder.png" : x.EventImg,
                    MaxSlot = x.EventMaxSlot,
                    Status = (int)(x.EventStatus ?? 0),
                    TenantPositionId = x.EventTenantPositionId
                })
                .ToListAsync();

            return vm;
        }

        // Overload cũ để code cũ vẫn chạy
        public Task<EventDetailsVm> GetEventByIdAsync(int eventId, int? currentUserId = null)
        {
            return GetEventByIdAsync(eventId, currentUserId, 1, 5);
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
                CustomerComplaintStatus = 0, // pending approval
                CustomerComplaintCreatedAt = DateTime.UtcNow
            };

            _context.TblCustomerComplaints.Add(ent);
            await _context.SaveChangesAsync();
        }

        // ADMIN CRUD
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
                .AsNoTracking()
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

        public async Task<IEnumerable<object>> GetCalendarEventsAsync()
        {
            var events = await _context.TblEvents
                .Include(e => e.EventTenantPosition)
                .Where(e => e.EventStatus == 1)
                .ToListAsync();

            return events.Select(e => new
            {
                id = e.EventId,
                title = e.EventName,
                start = e.EventStart,
                end = e.EventEnd,
                backgroundColor = "#4e73df",
                borderColor = "#4e73df",
                url = $"/Admin/Events/Details/{e.EventId}"
            });
        }

        public async Task<bool> CheckOverlapAsync(int positionId, DateTime start, DateTime end, int? excludeEventId = null)
        {
            var query = _context.TblEvents.AsNoTracking()
                .Where(e => e.EventTenantPositionId == positionId);

            if (excludeEventId.HasValue)
            {
                query = query.Where(e => e.EventId != excludeEventId.Value);
            }

            return await query.AnyAsync(e => e.EventStart < end && e.EventEnd > start);
        }

        // Partner
        public async Task<List<Event>> GetAllEventsByPositionId(int positionId)
        {
            return await _context.TblEvents
                .Where(x => x.EventTenantPositionId == positionId)
                .Select(x => new Event
                {
                    Id = x.EventId,
                    Name = x.EventName,
                    Img = x.EventImg,
                    Description = x.EventDescription,
                    Start = x.EventStart,
                    End = x.EventEnd,
                    Status = x.EventStatus ?? 0,
                    MaxSlot = x.EventMaxSlot,
                    UnitPrice = x.EventUnitPrice,
                    TenantPositionId = x.EventTenantPositionId,
                })
                .ToListAsync();
        }

        public async Task AddEvent(Event entity)
        {
            var item = new TblEvent
            {
                EventName = entity.Name,
                EventImg = entity.Img,
                EventDescription = entity.Description,
                EventStart = entity.Start,
                EventEnd = entity.End,
                EventStatus = entity.Status,
                EventMaxSlot = entity.MaxSlot,
                EventUnitPrice = entity.UnitPrice,
                EventTenantPositionId = entity.TenantPositionId,
            };
            _context.TblEvents.Add(item);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> DeleteEvent(int Id)
        {
            try
            {
                var item = await _context.TblEvents.FirstOrDefaultAsync(t => t.EventId == Id);
                if (item != null)
                {
                    _context.TblEvents.Remove(item);
                    return await _context.SaveChangesAsync() > 0;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdateEvent(Event entity)
        {
            var q = await _context.TblEvents.FirstOrDefaultAsync(t => t.EventId == entity.Id);
            if (q != null)
            {
                q.EventName = entity.Name;
                q.EventImg = entity.Img;
                q.EventDescription = entity.Description;
                q.EventStart = entity.Start;
                q.EventEnd = entity.End;
                q.EventStatus = entity.Status;
                q.EventMaxSlot = entity.MaxSlot;
                q.EventUnitPrice = entity.UnitPrice;
                q.EventTenantPositionId = entity.TenantPositionId;
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<Event?> FindById(int id)
        {
            return await _context.TblEvents
                .Where(t => t.EventId == id)
                .Select(t => new Event
                {
                    Id = t.EventId,
                    Name = t.EventName,
                    Status = t.EventStatus ?? 0,
                    Img = t.EventImg,
                    Description = t.EventDescription,
                    Start = t.EventStart,
                    End = t.EventEnd,
                    MaxSlot = t.EventMaxSlot,
                    UnitPrice = t.EventUnitPrice,
                    TenantPositionId = t.EventTenantPositionId
                })
                .FirstOrDefaultAsync();
        }

        public async Task<bool> CheckEventNameAsync(string name, int positionId, int? excludeId = null)
        {
            string normalizedInput = NormalizeName(name);

            var allEventsNames = await _context.TblEvents
                .Where(t =>
                    t.EventTenantPositionId == positionId &&
                    (!excludeId.HasValue || t.EventId != excludeId.Value)
                )
                .Select(t => t.EventName)
                .ToListAsync();

            return allEventsNames.Any(dbName => NormalizeName(dbName) == normalizedInput);
        }

        private string NormalizeName(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            return new string(input.Where(c => !char.IsWhiteSpace(c)).ToArray()).ToLowerInvariant();
        }

        public string NormalizeSearch(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            string lower = input.ToLowerInvariant();
            string normalized = lower.Normalize(NormalizationForm.FormD);

            StringBuilder sb = new StringBuilder();
            foreach (char c in normalized)
            {
                UnicodeCategory uc = CharUnicodeInfo.GetUnicodeCategory(c);
                if (uc != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }

            return new string(sb.ToString()
                .Where(c => !char.IsWhiteSpace(c))
                .ToArray());
        }
    }
}
