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
                ShortDescription = e.EventDescription.Length > 200 ? e.EventDescription.Substring(0, 197) + "..." : e.EventDescription,
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

        var q = _context.TblEvents
            .AsNoTracking()
            .Where(e => e.EventEnd >= now && e.EventStatus == 1)
            .OrderBy(e => e.EventStart)
            .Take(top)
            .Select(e => new EventCardVm
            {
                Id = e.EventId,
                Title = e.EventName,
                ShortDescription = e.EventDescription.Length > 200 ? e.EventDescription.Substring(0, 197) + "..." : e.EventDescription,
                StartDate = e.EventStart,
                EndDate = e.EventEnd,
                ImageUrl = string.IsNullOrEmpty(e.EventImg) ? "/images/event-placeholder.png" : e.EventImg,
                MaxSlot = e.EventMaxSlot,
                Status = (int)e.EventStatus,
                TenantPositionId = e.EventTenantPositionId
            });

        return await q.ToListAsync();
    }

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
            Description = e.EventDescription,
            StartDate = e.EventStart,
            EndDate = e.EventEnd,
            ImageUrl = string.IsNullOrEmpty(e.EventImg) ? "/images/event-placeholder.png" : e.EventImg,
            MaxSlot = e.EventMaxSlot,
            Status = (int)e.EventStatus,
            TenantPositionId = e.EventTenantPositionId
        };
    }
}
