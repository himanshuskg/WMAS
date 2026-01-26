using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.Blazor;
using WMAS.Data;
using WMAS.Models;

namespace WMAS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class DesignationController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DesignationController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.Designations.ToListAsync());
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Designation designation)
        {
            if (ModelState.IsValid)
            {
                _context.Designations.Add(designation);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(designation);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var designation = await _context.Designations.FindAsync(id);
            if (designation == null)
                return NotFound();

            return View(designation);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Designation designation)
        {
            if (id != designation.DesignationId)
                return NotFound();

            if (ModelState.IsValid)
            {
                _context.Update(designation);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(designation);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(int id)
        {
            var designation = await _context.Designations.FindAsync(id);
            if (designation == null) return NotFound();

            designation.IsActive = true;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Degignation activated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(int id)
        {
            var designation = await _context.Designations.FindAsync(id);
            if (designation == null) return NotFound();

            designation.IsActive = false;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Designation deactivated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var designation = await _context.Designations.FindAsync(id);
            if (designation == null) return NotFound();

            var inUse = await _context.Employees.AnyAsync(e => e.DesignationId == id);
            if (inUse)
            {
                TempData["Error"] = "Designation is assigned to employees and cannot be deleted. You may deactivate it instead.";
                return RedirectToAction(nameof(Details), new { id });
            }

            _context.Designations.Remove(designation);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Designation deleted.";
            return RedirectToAction(nameof(Index));
        }

    }
}
