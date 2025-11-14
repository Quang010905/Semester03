using Microsoft.AspNetCore.Mvc;
using Semester03.Models.Repositories;

namespace Semester03.Areas.Admin.Controllers
{
    public class TenantController : Controller
    {
        private readonly UserRepository _userRepo;

        // Inject repository qua constructor
        public TenantController(UserRepository userRepo)
        {
            _userRepo = userRepo ?? throw new ArgumentNullException(nameof(userRepo));
        }

        
    }
}
