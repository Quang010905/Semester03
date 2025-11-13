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
            // nếu danh sách rỗng thì tiếp tục để trả về default phía dưới
        }
        catch (Exception)
        {
            // Không ném tiếp để tránh crash view — có thể log ở đây nếu repository có ILogger injected.
            // Ví dụ: _logger?.LogError(ex, "Error fetching upcoming events");
        }

        // --- Trả về danh sách mặc định nếu DB rỗng hoặc có lỗi ---
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
