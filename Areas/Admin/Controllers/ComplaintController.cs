using Microsoft.AspNetCore.Mvc;
using Semester03.Models.Repositories;

namespace Semester03.Areas.Admin.Controllers
{
    [Area("Admin")]

    public class ComplaintController : Controller
    {

        private readonly CustomerComplaintRepository _cusRepo;


        public ComplaintController(CustomerComplaintRepository cusRepo)
        {
            _cusRepo = cusRepo;
        }



        public async Task<IActionResult> IndexAsync()    
        {
            var complaintTenant = await _cusRepo.GetTenantComplaintsAsync();
            ViewBag.ComplaintTenant = complaintTenant;

         


            var complaintMovie = await _cusRepo.GetMovieComplaintsAsync();
            ViewBag.ComplaintMovie = complaintMovie;


            var complaintEvent = await _cusRepo.GetEventComplaintsAsync(); 
            ViewBag.ComplaintEvent = complaintEvent;


            return View();
        }



        [HttpGet]
        public IActionResult Approve(int id)
        {
            _cusRepo.ApproveComplaint(id);
            return RedirectToAction("Index"); // Hoặc quay lại view hiện tại
        }

        [HttpGet]
        public IActionResult Cancel(int id)
        {
            _cusRepo.CancelComplaint(id);
            return RedirectToAction("Index");
        }



        [HttpPost]
        public IActionResult ApproveMultiple([FromBody] List<int> ids)
        {
            if (ids != null && ids.Count > 0)
            {
                _cusRepo.ApproveComplaints(ids);
            }
            return Ok();
        }

        [HttpPost]
        public IActionResult DeleteMultiple([FromBody] List<int> ids)
        {
            if (ids != null && ids.Count > 0)
            {
                _cusRepo.DeleteComplaints(ids);
            }
            return Ok();
        }
    }









}
