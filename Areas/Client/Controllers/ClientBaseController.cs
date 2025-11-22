using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Semester03.Models.Repositories;
using System.Threading.Tasks;

namespace Semester03.Areas.Client.Controllers
{
    public class ClientBaseController : Controller
    {
        protected readonly TenantTypeRepository _tenantTypeRepo; // 👈 sửa từ private → protected

        public ClientBaseController(TenantTypeRepository tenantTypeRepo)
        {
            _tenantTypeRepo = tenantTypeRepo;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            ViewBag.StoreTypes = await _tenantTypeRepo.GetActiveTenantType();
            await base.OnActionExecutionAsync(context, next);
        }
    }

}
