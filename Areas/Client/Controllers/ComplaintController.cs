using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Semester03.Areas.Client.Models.ViewModels;
using Semester03.Models.Entities;
using Semester03.Models.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Semester03.Areas.Client.Controllers
{
    [Area("Client")]
    [Authorize]
    // BẮT BUỘC route dạng /Client/Complaint/MyComplaints
    [Route("Client/[controller]/[action]")]
    public class ComplaintController : ClientBaseController
    {
        private readonly AbcdmallContext _context;

        public ComplaintController(
            TenantTypeRepository tenantTypeRepo,
            AbcdmallContext context
        ) : base(tenantTypeRepo)
        {
            _context = context;
        }

        // GET: /Client/Complaint/MyComplaints
        [HttpGet]
        public async Task<IActionResult> MyComplaints()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr))
            {
                return RedirectToAction(
                    "Login",
                    "Account",
                    new
                    {
                        area = "Client",
                        returnUrl = Url.Action("MyComplaints", "Complaint", new { area = "Client" })
                    }
                );
            }

            int userId = int.Parse(userIdStr);

            var complaints = await _context.TblCustomerComplaints
                .AsNoTracking()
                .Where(c => c.CustomerComplaintCustomerUserId == userId)
                .OrderByDescending(c => c.CustomerComplaintCreatedAt)
                .ToListAsync();

            var vms = new List<MyComplaintVm>();

            foreach (var c in complaints)
            {
                string targetType = "Other";
                string targetName = "-";

                if (c.CustomerComplaintMovieId.HasValue && c.CustomerComplaintMovieId.Value != 0)
                {
                    targetType = "Movie";
                    var movie = await _context.TblMovies
                        .AsNoTracking()
                        .FirstOrDefaultAsync(m => m.MovieId == c.CustomerComplaintMovieId.Value);
                    targetName = movie?.MovieTitle ?? $"Movie #{c.CustomerComplaintMovieId.Value}";
                }
                else if (c.CustomerComplaintEventId.HasValue && c.CustomerComplaintEventId.Value != 0)
                {
                    targetType = "Event";
                    var evt = await _context.TblEvents
                        .AsNoTracking()
                        .FirstOrDefaultAsync(e => e.EventId == c.CustomerComplaintEventId.Value);
                    targetName = evt?.EventName ?? $"Event #{c.CustomerComplaintEventId.Value}";
                }
                else if (c.CustomerComplaintTenantId.HasValue && c.CustomerComplaintTenantId.Value != 0)
                {
                    targetType = "Store";
                    var tenant = await _context.TblTenants
                        .AsNoTracking()
                        .FirstOrDefaultAsync(t => t.TenantId == c.CustomerComplaintTenantId.Value);
                    targetName = tenant?.TenantName ?? $"Store #{c.CustomerComplaintTenantId.Value}";
                }

                string statusLabel = c.CustomerComplaintStatus switch
                {
                    1 => "Approved",
                    2 => "Rejected",
                    _ => "Pending"
                };

                string shortContent = c.CustomerComplaintDescription ?? "";
                if (shortContent.Length > 140)
                    shortContent = shortContent.Substring(0, 137) + "...";

                vms.Add(new MyComplaintVm
                {
                    Id = c.CustomerComplaintId,
                    TargetType = targetType,
                    TargetName = targetName,
                    Rate = c.CustomerComplaintRate,
                    StatusLabel = statusLabel,
                    CreatedAt = c.CustomerComplaintCreatedAt ?? DateTime.MinValue,
                    ShortContent = shortContent,
                    DetailUrl = ""
                });
            }

            return View(vms);
        }
    }
}
