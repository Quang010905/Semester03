using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Semester03.Models.Entities;
using Semester03.Models.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Semester03.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ScreensController : Controller
    {
        private readonly ScreenRepository _screenRepo;

        // This is the hard-coded ID of your one-and-only Cinema
        private const int FIXED_CINEMA_ID = 1;

        public ScreensController(ScreenRepository screenRepo)
        {
            _screenRepo = screenRepo;
        }

        // GET: Admin/Screens
        public async Task<IActionResult> Index()
        {
            var screens = await _screenRepo.GetAllAsync();
            return View(screens);
        }

        // GET: Admin/Screens/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var screen = await _screenRepo.GetByIdAsync(id.Value);
            if (screen == null) return NotFound();
            return View(screen);
        }

        // GET: Admin/Screens/Create
        public IActionResult Create()
        {
            // We don't need a dropdown, we will set the Cinema ID automatically
            return View();
        }

        // POST: Admin/Screens/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("ScreenName,ScreenSeats")] TblScreen tblScreen)
        {
            ModelState.Remove("ScreenCinemaId");
            ModelState.Remove("ScreenCinema");

            // Re-check model state after adding the ID
            if (ModelState.IsValid)
            {
                // Manually set the Cinema ID
                tblScreen.ScreenCinemaId = FIXED_CINEMA_ID;
                await _screenRepo.AddAsync(tblScreen);
                return RedirectToAction(nameof(Index));
            }
            return View(tblScreen);
        }

        // GET: Admin/Screens/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var screen = await _screenRepo.GetByIdAsync(id.Value);
            if (screen == null) return NotFound();
            return View(screen);
        }

        // POST: Admin/Screens/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id,
            [Bind("ScreenId,ScreenName,ScreenSeats")] TblScreen tblScreen)
        {
            ModelState.Remove("ScreenCinemaId");
            ModelState.Remove("ScreenCinema");
            if (id != tblScreen.ScreenId) return NotFound();

            

            if (ModelState.IsValid)
            {
                try
                {
                    // Manually set the Cinema ID again to ensure it's correct
                    tblScreen.ScreenCinemaId = FIXED_CINEMA_ID;
                    await _screenRepo.UpdateAsync(tblScreen);
                }
                catch (DbUpdateConcurrencyException)
                {
                    var exists = await _screenRepo.GetByIdAsync(id);
                    if (exists == null) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(tblScreen);
        }

        // GET: Admin/Screens/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var screen = await _screenRepo.GetByIdAsync(id.Value);
            if (screen == null) return NotFound();
            return View(screen);
        }

        // POST: Admin/Screens/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _screenRepo.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    

}
}
