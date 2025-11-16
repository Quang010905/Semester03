using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Semester03.Models.Entities;
using Semester03.Models.Repositories;

namespace Semester03.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "1")]
    public class CouponsController : Controller
    {
        private readonly CouponRepository _couponRepo;
        private readonly AbcdmallContext _context; // For dependency check

        public CouponsController(CouponRepository couponRepo, AbcdmallContext context)
        {
            _couponRepo = couponRepo;
            _context = context;
        }

        // GET: Admin/Coupons
        public async Task<IActionResult> Index()
        {
            var coupons = await _couponRepo.GetAllAsync();
            return View(coupons);
        }

        // GET: Admin/Coupons/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var coupon = await _couponRepo.GetByIdAsync(id.Value);
            if (coupon == null) return NotFound();
            return View(coupon);
        }

        // GET: Admin/Coupons/Create
        public IActionResult Create()
        {
            
            // We must round the time to avoid the browser step validation error.
            var now = DateTime.Now;

            // Round down to the current minute (e.g., 4:31:53 -> 4:31:00)
            var defaultStart = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0);

            var model = new TblCoupon
            {
                CouponIsActive = true,
                CouponValidFrom = defaultStart, // Use the "clean" value
                CouponValidTo = defaultStart.AddDays(30) // Use the "clean" value
            };
            return View(model);
        }

        // POST: Admin/Coupons/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("CouponName,CouponDescription,CouponDiscountPercent,CouponValidFrom,CouponValidTo,CouponIsActive")] TblCoupon tblCoupon)
        {
            // --- Business Logic Validation ---
            if (tblCoupon.CouponDiscountPercent <= 0)
            {
                ModelState.AddModelError("CouponDiscountPercent", "Discount Percent must be greater than 0.");
            }
            if (tblCoupon.CouponValidTo <= tblCoupon.CouponValidFrom)
            {
                ModelState.AddModelError("CouponValidTo", "Valid To date must be after Valid From date.");
            }
            if (tblCoupon.CouponValidFrom.Date < DateTime.Now.Date)
            {
                ModelState.AddModelError("CouponValidFrom", "Valid From date cannot be in the past.");
            }
            // --- END VALIDATION ---

            if (ModelState.IsValid)
            {
                await _couponRepo.AddAsync(tblCoupon);
                return RedirectToAction(nameof(Index));
            }
            return View(tblCoupon);
        }

        // GET: Admin/Coupons/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var coupon = await _couponRepo.GetByIdAsync(id.Value);
            if (coupon == null) return NotFound();
            return View(coupon);
        }

        // POST: Admin/Coupons/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id,
            [Bind("CouponId,CouponName,CouponDescription,CouponDiscountPercent,CouponValidFrom,CouponValidTo,CouponIsActive")] TblCoupon tblCoupon)
        {
            if (id != tblCoupon.CouponId) return NotFound();

            // --- ADDED: Business Logic Validation ---
            if (tblCoupon.CouponDiscountPercent <= 0)
            {
                ModelState.AddModelError("CouponDiscountPercent", "Discount Percent must be greater than 0.");
            }
            if (tblCoupon.CouponValidTo <= tblCoupon.CouponValidFrom)
            {
                ModelState.AddModelError("CouponValidTo", "Valid To date must be after Valid From date.");
            }
            // (We allow editing 'ValidFrom' to a past date, in case the coupon is already active)
            // --- END VALIDATION ---

            if (ModelState.IsValid)
            {
                try
                {
                    await _couponRepo.UpdateAsync(tblCoupon);
                }
                catch (DbUpdateConcurrencyException)
                {
                    var exists = await _couponRepo.GetByIdAsync(id);
                    if (exists == null) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(tblCoupon);
        }

        // GET: Admin/Coupons/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var coupon = await _couponRepo.GetByIdAsync(id.Value);
            if (coupon == null) return NotFound();

            // Check for dependencies (Tbl_CouponUser)
            bool hasUsers = await _context.TblCouponUsers.AnyAsync(cu => cu.CouponId == id);
            if (hasUsers)
            {
                ViewData["HasDependencies"] = true;
                ViewData["ErrorMessage"] = "This coupon cannot be deleted. It has been assigned to one or more users. Please set it to 'Inactive' instead.";
            }

            return View(coupon);
        }

        // POST: Admin/Coupons/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            bool hasUsers = await _context.TblCouponUsers.AnyAsync(cu => cu.CouponId == id);
            if (hasUsers)
            {
                TempData["Error"] = "This coupon cannot be deleted (it is assigned to users).";
                return RedirectToAction(nameof(Index));
            }

            await _couponRepo.DeleteAsync(id);
            TempData["Success"] = "Coupon deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
