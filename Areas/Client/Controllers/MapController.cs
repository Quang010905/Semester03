using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Semester03.Models.Repositories;
using Semester03.Areas.Client.Models.ViewModels;

namespace Semester03.Areas.Client.Controllers
{
    [Area("Client")]
    public class MapController : Controller
    {
        private readonly TenantPositionRepository _posRepo;
        private readonly IWebHostEnvironment _env;

        public MapController(TenantPositionRepository posRepo, IWebHostEnvironment env)
        {
            _posRepo = posRepo ?? throw new ArgumentNullException(nameof(posRepo));
            _env = env ?? throw new ArgumentNullException(nameof(env));
        }

        // GET /Client/Map?floor=1
        public async Task<IActionResult> Index(int floor = 1)
        {
            var vm = new MapViewModel();
            vm.FloorNumber = floor;
            vm.Columns = 8;

            // image path expected under wwwroot/Content/Uploads/FloorImg/floor{n}.png
            var rel = $"/Content/Uploads/FloorImg/floor{floor}.png";
            var physical = Path.Combine(_env.WebRootPath ?? "wwwroot", "Content", "Uploads", "FloorImg", $"floor{floor}.png");
            vm.FloorImagePath = System.IO.File.Exists(physical) ? rel : null;

            // load positions
            var positions = await _posRepo.GetPositionsByFloorAsync(floor);
            vm.Positions = positions;

            // basic computed metrics (example)
            vm.MaxPositionsComputed = positions.Count; // or compute from floor area / pos area if you store them
            vm.RenderedCellCount = positions.Count;

            // populate example area fields (optional)
            vm.FloorAreaM2 = (decimal)(positions.Sum(p => (double?)p.TenantPosition_Area_M2 ?? 0) == 0 ? 7000 : positions.Sum(p => (double?)p.TenantPosition_Area_M2 ?? 0));
            vm.ReservedAreaM2 = 1500; // sample
            vm.PositionAreaM2 = positions.FirstOrDefault()?.TenantPosition_Area_M2 ?? 150;

            return View(vm);
        }

        // GET: /Client/Map/GetPositionJson?id=123
        public async Task<IActionResult> GetPositionJson(int id)
        {
            var dto = await _posRepo.GetByIdAsync(id);
            if (dto == null) return NotFound();
            return Json(dto);
        }

        // POST delete
        [HttpPost]
        public async Task<IActionResult> DeletePosition(int id)
        {
            try
            {
                await _posRepo.DeleteAsync(id);
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> SavePositionCoords(int id, decimal leftPct, decimal topPct)
        {
            try
            {
                // validate range
                if (leftPct < 0 || leftPct > 100 || topPct < 0 || topPct > 100)
                    return BadRequest("Coordinates must be between 0 and 100.");

                // collision check (server-side)
                var hasCollision = await _posRepo.HasNearbyPositionAsync(id, leftPct, topPct, 6M); // 6% threshold (tuneable)
                if (hasCollision)
                {
                    return Conflict("Collision: vị trí mới quá gần vị trí khác. Vui lòng chọn vị trí khác.");
                }

                var ok = await _posRepo.UpdatePositionCoordsAsync(id, leftPct, topPct);
                if (!ok) return NotFound("Không tìm thấy vị trí để cập nhật.");

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
