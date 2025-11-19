using Semester03.Areas.Client.Models.ViewModels;
using Semester03.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Semester03.Models.Repositories;
public class EventRepository
{
    private readonly AbcdmallContext _context;

    public EventRepository(AbcdmallContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<List<EventCardVm>> GetFeaturedEventsAsync(int top = 3)
    {
        var now = DateTime.Now;

        var q = _context.TblEvents
            .AsNoTracking()
            .Where(e => e.EventStart >= now && e.EventStatus == 1)
            .OrderBy(e => e.EventStart)
            .Take(top)
            .Select(e => new EventCardVm
            {
                Id = e.EventId,
                Title = e.EventName,
                ShortDescription = string.IsNullOrEmpty(e.EventDescription) ? "" :
                    (e.EventDescription.Length > 200 ? e.EventDescription.Substring(0, 197) + "..." : e.EventDescription),
                StartDate = e.EventStart,
                EndDate = e.EventEnd,
                ImageUrl = string.IsNullOrEmpty(e.EventImg) ? "/images/event-placeholder.png" : e.EventImg,
                MaxSlot = e.EventMaxSlot,
                Status = (int)e.EventStatus,
                TenantPositionId = e.EventTenantPositionId
            });

        return await q.ToListAsync();
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

    public async Task<EventDetailsVm> GetEventByIdAsync(int eventId)
    {
        var e = await _context.TblEvents
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.EventId == eventId);

        if (e == null) return null;

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
            TenantPositionId = e.EventTenantPositionId // assuming it's int
        };

        // Lấy Sự kiện liên quan: nếu tenantId != 0 thì lấy, ngược lại bỏ qua
        try
        {
            if (e.EventTenantPositionId != 0)
            {
                vm.Related = await GetRelatedEventsAsync(e.EventTenantPositionId, eventId, 8);
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
    /// Lấy các sự kiện liên quan (cùng tenant). Loại bỏ event hiện tại (excludeId).
    /// </summary>
    public async Task<List<EventCardVm>> GetRelatedEventsAsync(int tenantId, int excludeId, int take = 4)
    {
        var now = DateTime.Now;

        var q = _context.TblEvents
            .AsNoTracking()
            .Where(e => e.EventTenantPositionId == tenantId && e.EventId != excludeId && e.EventStatus == 1 && e.EventEnd >= now)
            .OrderBy(e => e.EventStart)
            .Take(take)
            .Select(e => new EventCardVm
            {
                Id = e.EventId,
                Title = e.EventName,
                ShortDescription = string.IsNullOrEmpty(e.EventDescription) ? "" :
                    (e.EventDescription.Length > 120 ? e.EventDescription.Substring(0, 117) + "..." : e.EventDescription),
                StartDate = e.EventStart,
                EndDate = e.EventEnd,
                ImageUrl = string.IsNullOrEmpty(e.EventImg) ? "/images/event-placeholder.png" : e.EventImg,
                MaxSlot = e.EventMaxSlot,
                Status = (int)e.EventStatus,
                TenantPositionId = e.EventTenantPositionId
            });

        var list = await q.ToListAsync();

        // Nếu không có related theo tenant, fallback lấy upcoming khác
        if (list == null || !list.Any())
        {
            var fallback = await _context.TblEvents
                .AsNoTracking()
                .Where(e => e.EventId != excludeId && e.EventStatus == 1 && e.EventEnd >= now)
                .OrderBy(e => e.EventStart)
                .Take(take)
                .Select(e => new EventCardVm
                {
                    Id = e.EventId,
                    Title = e.EventName,
                    ShortDescription = string.IsNullOrEmpty(e.EventDescription) ? "" :
                        (e.EventDescription.Length > 120 ? e.EventDescription.Substring(0, 117) + "..." : e.EventDescription),
                    StartDate = e.EventStart,
                    EndDate = e.EventEnd,
                    ImageUrl = string.IsNullOrEmpty(e.EventImg) ? "/images/event-placeholder.png" : e.EventImg,
                    MaxSlot = e.EventMaxSlot,
                    Status = (int)e.EventStatus,
                    TenantPositionId = e.EventTenantPositionId
                })
                .ToListAsync();

            return fallback ?? new List<EventCardVm>();
        }

        return list;
    }

    // ==========================================================
    // ADMIN CRUD METHODS 
    // ==========================================================

    public async Task<IEnumerable<TblEvent>> GetAllAsync()
    {
        // Include related position data
        return await _context.TblEvents
            .Include(e => e.EventTenantPosition)
            .OrderByDescending(e => e.EventStart)
            .ToListAsync();
    }

    public async Task<TblEvent> GetByIdAdminAsync(int id)
    {
        // Use this one for Admin (GetEventByIdAsync is already used by Client)
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
