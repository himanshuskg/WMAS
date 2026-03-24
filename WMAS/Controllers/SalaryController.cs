using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMAS.Data;
using WMAS.Models;

namespace WMAS.Controllers
{
    public class SalaryController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public SalaryController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ── ADMIN: All salary records ─────────────────────────────────────
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index(int? month, int? year, int? employeeId)
        {
            int m = month ?? DateTime.Today.Month;
            int y = year ?? DateTime.Today.Year;

            var query = _context.Salaries
                .Include(s => s.Employee)
                .Where(s => s.Month == m && s.Year == y);

            if (employeeId.HasValue)
                query = query.Where(s => s.EmployeeId == employeeId.Value);

            var records = await query
                .OrderBy(s => s.Employee.FullName)
                .ToListAsync();

            ViewBag.Employees = await _context.Employees
                .Where(e => e.IsActive)
                .OrderBy(e => e.FullName)
                .ToListAsync();

            ViewBag.Month = m;
            ViewBag.Year = y;
            ViewBag.EmployeeId = employeeId;

            // Summary
            ViewBag.TotalBasic = records.Sum(s => s.BasicPay);
            ViewBag.TotalNet = records.Sum(s => s.NetSalary);
            ViewBag.TotalDeduct = records.Sum(s => s.Deductions);

            return View(records);
        }

        // ── ADMIN: Generate payroll for all active employees for a month ──
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateBulk(int month, int year)
        {
            var employees = await _context.Employees
                .Where(e => e.IsActive)
                .ToListAsync();

            int created = 0;
            foreach (var emp in employees)
            {
                var exists = await _context.Salaries
                    .AnyAsync(s => s.EmployeeId == emp.EmployeeId
                               && s.Month == month
                               && s.Year == year);
                if (exists) continue;

                _context.Salaries.Add(new Salary
                {
                    EmployeeId = emp.EmployeeId,
                    BasicPay = emp.Salary,
                    Allowances = 0,
                    Deductions = 0,
                    NetSalary = emp.Salary,
                    Month = month,
                    Year = year
                });
                created++;
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Generated payroll for {created} employee(s). Already existing records were skipped.";
            return RedirectToAction(nameof(Index), new { month, year });
        }

        // ── ADMIN: Create / Edit single salary record ─────────────────────
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Upsert(int? id, int? employeeId, int? month, int? year)
        {
            ViewBag.Employees = await _context.Employees
                .Where(e => e.IsActive)
                .OrderBy(e => e.FullName)
                .ToListAsync();

            if (id.HasValue)
            {
                var existing = await _context.Salaries
                    .Include(s => s.Employee)
                    .FirstOrDefaultAsync(s => s.SalaryId == id.Value);

                if (existing == null) return NotFound();
                return View(existing);
            }

            // New
            var salary = new Salary
            {
                EmployeeId = employeeId ?? 0,
                Month = month ?? DateTime.Today.Month,
                Year = year ?? DateTime.Today.Year,
                BasicPay = 0,
                Allowances = 0,
                Deductions = 0,
                NetSalary = 0
            };

            // Pre-fill from employee salary field
            if (employeeId.HasValue)
            {
                var emp = await _context.Employees.FindAsync(employeeId.Value);
                if (emp != null) salary.BasicPay = emp.Salary;
            }

            return View(salary);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upsert(Salary salary)
        {
            // Auto-compute NetSalary
            salary.NetSalary = salary.BasicPay + salary.Allowances - salary.Deductions;

            if (!ModelState.IsValid)
            {
                ViewBag.Employees = await _context.Employees
                    .Where(e => e.IsActive)
                    .OrderBy(e => e.FullName)
                    .ToListAsync();
                return View(salary);
            }

            // Check duplicate
            var duplicate = await _context.Salaries.AnyAsync(s =>
                s.EmployeeId == salary.EmployeeId &&
                s.Month == salary.Month &&
                s.Year == salary.Year &&
                s.SalaryId != salary.SalaryId);

            if (duplicate)
            {
                ModelState.AddModelError("", "A salary record already exists for this employee for the selected month/year.");
                ViewBag.Employees = await _context.Employees
                    .Where(e => e.IsActive)
                    .OrderBy(e => e.FullName)
                    .ToListAsync();
                return View(salary);
            }

            if (salary.SalaryId == 0)
                _context.Salaries.Add(salary);
            else
                _context.Salaries.Update(salary);

            await _context.SaveChangesAsync();

            TempData["Success"] = salary.SalaryId == 0
                ? "Salary record created."
                : "Salary record updated.";

            return RedirectToAction(nameof(Index),
                new { month = salary.Month, year = salary.Year });
        }

        // ── EMPLOYEE: My salary slips ─────────────────────────────────────
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> MySlips(int? year)
        {
            var userId = _userManager.GetUserId(User);
            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.UserId == userId);

            if (employee == null) return Unauthorized();

            int y = year ?? DateTime.Today.Year;

            var slips = await _context.Salaries
                .Where(s => s.EmployeeId == employee.EmployeeId && s.Year == y)
                .OrderBy(s => s.Month)
                .ToListAsync();

            ViewBag.Year = y;
            ViewBag.Employee = employee;

            return View(slips);
        }
    }
}
