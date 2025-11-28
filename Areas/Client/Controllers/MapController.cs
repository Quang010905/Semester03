using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Semester03.Models.Repositories;
using Semester03.Areas.Client.Models.ViewModels;
using System.Collections.Generic;

namespace Semester03.Areas.Client.Controllers
{
    [Area("Client")]
    public class MapController : ClientBaseController
    {
        private readonly TenantPositionRepository _posRepo;
        private readonly IWebHostEnvironment _env;

        public MapController(
            TenantTypeRepository tenantTypeRepo,
            TenantPositionRepository posRepo,
            IWebHostEnvironment env
        ) : base(tenantTypeRepo)
        {
            _posRepo = posRepo ?? throw new ArgumentNullException(nameof(posRepo));
            _env = env ?? throw new ArgumentNullException(nameof(env));
        }

        // GET /Client/Map?floor=1
        public async Task<IActionResult> Index(int floor = 1) // default Floor 1 (no more ground 0 retail)
        {
            var vm = new MapViewModel
            {
                FloorNumber = floor,
                Columns = 8
            };

            // floor image (optional)
            var rel = $"/Content/Uploads/FloorImg/floor{floor}.png";
            var physical = Path.Combine(_env.WebRootPath ?? "wwwroot", "Content", "Uploads", "FloorImg", $"floor{floor}.png");
            vm.FloorImagePath = System.IO.File.Exists(physical) ? rel : null;

            // ----- RETAIL FLOORS (4-storeyed mall) -----
            var floors = new List<int>();
            var allPositions = new List<TenantPositionDto>();

            int minRetailFloor = 1;
            int maxRetailFloor = 4; // theo đề bài: 4 tầng thương mại

            for (int f = minRetailFloor; f <= maxRetailFloor; f++)
            {
                var count = await _posRepo.GetCountByFloorAsync(f);
                if (count > 0)
                {
                    floors.Add(f);
                    var list = await _posRepo.GetPositionsByFloorWithTenantAsync(f);
                    if (list != null && list.Any())
                        allPositions.AddRange(list);
                }
            }

            // nếu vì lý do nào đó vẫn chưa có dữ liệu, cố gắng load floor user yêu cầu
            if (!floors.Any())
            {
                var single = await _posRepo.GetPositionsByFloorWithTenantAsync(floor);
                if (single != null && single.Any())
                {
                    floors.Add(floor);
                    allPositions.AddRange(single);
                }
            }

            vm.AvailableFloors = floors.Any()
                ? floors.OrderBy(x => x).ToList()
                : new List<int> { 1 };

            if (!vm.AvailableFloors.Contains(floor))
            {
                vm.AvailableFloors = vm.AvailableFloors
                    .Concat(new[] { floor })
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList();
            }

            vm.Positions = allPositions;
            vm.MaxPositionsComputed = vm.Positions.Count;
            vm.RenderedCellCount = vm.Positions.Count;

            var sumArea = vm.Positions.Sum(p => (double?)p.TenantPosition_Area_M2 ?? 0);
            vm.FloorAreaM2 = (decimal)(sumArea == 0 ? 7000 : sumArea);
            vm.ReservedAreaM2 = 1500;
            vm.PositionAreaM2 = vm.Positions.FirstOrDefault()?.TenantPosition_Area_M2 ?? 150;

            ViewData["PositionsCount"] = vm.Positions.Count;

            // ----- PARKING (7-level car park) -----
            var parkingLevels = await _posRepo.GetParkingLevelsWithSpotsAsync();
            vm.ParkingLevels = parkingLevels ?? new List<ParkingLevelDto>();
            vm.CurrentParkingLevelId = vm.ParkingLevels.FirstOrDefault()?.LevelId;

            return View("~/Areas/Client/Views/Map/Index.cshtml", vm);
        }

        // 3D view vẫn giữ nguyên (nếu bạn đang dùng)
        [HttpGet]
        public async Task<IActionResult> Index3D(int floor = 1)
        {
            var vm = new MapViewModel
            {
                FloorNumber = floor,
                Columns = 8
            };

            var rel = $"/Content/Uploads/FloorImg/floor{floor}.png";
            var physical = Path.Combine(_env.WebRootPath ?? "wwwroot", "Content", "Uploads", "FloorImg", $"floor{floor}.png");
            vm.FloorImagePath = System.IO.File.Exists(physical) ? rel : null;

            var allPositions = new List<TenantPositionDto>();
            var floorsToLoad = new[] { 1, 2, 3, 4 };
            foreach (var f in floorsToLoad)
            {
                var list = await _posRepo.GetPositionsByFloorAsync(f) ?? new List<TenantPositionDto>();
                if (list.Any()) allPositions.AddRange(list);
            }

            vm.Positions = allPositions;
            vm.MaxPositionsComputed = vm.Positions.Count;
            vm.RenderedCellCount = vm.Positions.Count;
            var sumArea = vm.Positions.Sum(p => (double?)p.TenantPosition_Area_M2 ?? 0);
            vm.FloorAreaM2 = (decimal)(sumArea == 0 ? 7000 : sumArea);
            vm.ReservedAreaM2 = 1500;
            vm.PositionAreaM2 = vm.Positions.FirstOrDefault()?.TenantPosition_Area_M2 ?? 150;

            return View("~/Areas/Client/Views/Map/Index3D.cshtml", vm);
        }

        // giữ nguyên các action dưới
        public async Task<IActionResult> GetPositionJson(int id)
        {
            var dto = await _posRepo.GetByIdAsync(id);
            if (dto == null) return NotFound();
            return Json(dto);
        }

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
                if (leftPct < 0 || leftPct > 100 || topPct < 0 || topPct > 100)
                    return BadRequest("Coordinates must be between 0 and 100.");

                var hasCollision = await _posRepo.HasNearbyPositionAsync(id, leftPct, topPct, 6M);
                if (hasCollision)
                    return Conflict("Collision: vị trí mới quá gần vị trí khác. Vui lòng chọn vị trí khác.");

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
