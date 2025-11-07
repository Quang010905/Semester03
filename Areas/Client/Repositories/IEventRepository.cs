using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Semester03.Areas.Client.Models.ViewModels;

namespace Semester03.Areas.Client.Repositories
{
    public interface IEventRepository
    {
        Task<List<EventCardVm>> GetFeaturedEventsAsync(int top = 3);
        Task<List<EventCardVm>> GetUpcomingEventsAsync();
        Task<EventDetailsVm> GetEventByIdAsync(int eventId);
    }
}
