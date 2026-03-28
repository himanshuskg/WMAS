using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMAS.Data;
using WMAS.Models;

namespace WMAS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class EmployeeTypeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EmployeeTypeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var types = await _context.EmployeeTypes
                .OrderBy(t => t.Name)
                .ToListAsync();
            return View(types);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["Error"] = "Name cannot be empty.";
                return RedirectToAction(nameof(Index));
            }

            bool exists = await _context.EmployeeTypes
                .AnyAsync(t => t.Name.ToLower() == name.ToLower().Trim());

            if (exists)
            {
                TempData["Error"] = $"Employee type '{name}' already exists.";
                return RedirectToAction(nameof(Index));
            }

            _context.EmployeeTypes.Add(new EmployeeType
            {
                Name = name.Trim()
            });

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Employee type '{name}' added.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["Error"] = "Name cannot be empty.";
                return RedirectToAction(nameof(Index));
            }

            var type = await _context.EmployeeTypes.FindAsync(id);
            if (type == null) return NotFound();

            bool duplicate = await _context.EmployeeTypes
                .AnyAsync(t => t.Name.ToLower() == name.ToLower().Trim()
                            && t.Id != id);

            if (duplicate)
            {
                TempData["Error"] = $"'{name}' already exists.";
                return RedirectToAction(nameof(Index));
            }

            type.Name = name.Trim();
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Updated to '{name}'.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var type = await _context.EmployeeTypes.FindAsync(id);
            if (type == null) return NotFound();

            // Check if any employee uses this type
            bool inUse = await _context.Employees
                .AnyAsync(e => e.EmployeeTypeId == id);

            if (inUse)
            {
                TempData["Error"] =
                    $"Cannot delete '{type.Name}' — it is assigned to employees.";
                return RedirectToAction(nameof(Index));
            }

            _context.EmployeeTypes.Remove(type);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"'{type.Name}' deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}