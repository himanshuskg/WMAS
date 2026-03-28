using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.Blazor;
using WMAS.Data;
using WMAS.Models;

namespace WMAS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class DepartmentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DepartmentController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.Departments.ToListAsync());
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Department department)
        {
            if (ModelState.IsValid)
            {
                _context.Departments.Add(department);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(department);
        }
        public async Task<IActionResult> Edit(int id)
        {
            var department = await _context.Departments.FindAsync(id);
            if (department == null)
                return NotFound();

            return View(department);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Department department)
        {
            if (id != department.DepartmentId)
                return NotFound();

            if (ModelState.IsValid)
            {
                _context.Update(department);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(department);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(int id)
        {
            var dept = await _context.Departments.FindAsync(id);
            if (dept == null) return NotFound();

            dept.IsActive = true;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Department activated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(int id)
        {
            var dept = await _context.Departments.FindAsync(id);
            if (dept == null) return NotFound();

            dept.IsActive = false;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Department deactivated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var dept = await _context.Departments.FindAsync(id);
            if (dept == null) return NotFound();

            var inUse = await _context.Employees.AnyAsync(e => e.DepartmentId == id);
            if (inUse)
            {
                TempData["Error"] = "Department is assigned to employees and cannot be deleted. You may deactivate it instead.";
                return RedirectToAction(nameof(Details), new { id });
            }

            _context.Departments.Remove(dept);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Department deleted.";
            return RedirectToAction(nameof(Index));
        }


    }
}
