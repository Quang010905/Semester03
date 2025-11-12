using Semester03.Models.ViewModels;

namespace Semester03.Areas.Client.Models.ViewModels
{
    public class HomeMaillViewModel
    {
       
        // Danh sách sự kiện sắp tới (Event)
        public IEnumerable<EventCardVm> UpcomingEvents { get; set; } = new List<EventCardVm>();

        // Danh sách tenant nổi bật (Cửa hàng)
        public IEnumerable<FeaturedStoreViewModel> FeaturedStores { get; set; } = new List<FeaturedStoreViewModel>();
    }
}
