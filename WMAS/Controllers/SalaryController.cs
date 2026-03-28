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

        private int GetTotalDaysInMonth(int year, int month)
            => DateTime.DaysInMonth(year, month);

        private int GetSundaysInMonth(int year, int month)
        {
            int total = DateTime.DaysInMonth(year, month);
            int sundays = 0;
            for (int d = 1; d <= total; d++)
                if (new DateTime(year, month, d).DayOfWeek == DayOfWeek.Sunday)
                    sundays++;
            return sundays;
        }

        private int GetExpectedWorkingDays(int year, int month)
            => GetTotalDaysInMonth(year, month) - GetSundaysInMonth(year, month);

        private async Task<(decimal PaidDays, int PresentFull, int PresentHalf, int ApprovedLeaveDays, decimal LopDays)> GetAttendanceSummary(int employeeId, int month, int year)
        {
            int expectedWorkingDays = GetExpectedWorkingDays(year, month);

            var monthStart = new DateTime(year, month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            // Fetch from DB first — no DayOfWeek in SQL
            var attendances = await _context.Attendances.Where(a => a.EmployeeId == employeeId && a.Status == "Present" && a.Date >= (monthStart) && a.Date <= (monthEnd)).ToListAsync();

            // Filter Sundays in-memory (safe, max 31 records per employee)
            attendances = attendances.Where(a => a.Date.DayOfWeek != DayOfWeek.Sunday).ToList();

            const int minWorkMinutes = 7 * 60 + 45; // 7h 45m minimum
            int fullDays = 0, halfDays = 0;

            foreach (var a in attendances)
            {
                if (a.CheckInTime == null || a.CheckOutTime == null)
                {
                    halfDays++; // no checkout = half day
                    continue;
                }

                double workedMinutes = (a.CheckOutTime.Value - a.CheckInTime.Value).TotalMinutes;

                if (workedMinutes >= minWorkMinutes)
                    fullDays++;
                else
                    halfDays++;
            }

            // Approved leave days (Mon–Sat only, overlapping this month)
            var approvedLeaves = await _context.Leaves
                .Where(l => l.EmployeeId == employeeId
                         && l.Status == "Approved"
                         && l.FromDate <= monthEnd
                         && l.ToDate >= monthStart)
                .ToListAsync();

            int approvedLeaveDays = 0;
            foreach (var leave in approvedLeaves)
            {
                var from = leave.FromDate < monthStart ? monthStart : leave.FromDate;
                var to = leave.ToDate > monthEnd ? monthEnd : leave.ToDate;

                for (var date = from; date <= to; date = date.AddDays(1))
                    if (date.DayOfWeek != DayOfWeek.Sunday)
                        approvedLeaveDays++;
            }

            decimal paidDays = fullDays + (halfDays * 0.5m) + approvedLeaveDays;

            // Cap at expected working days
            if (paidDays > expectedWorkingDays)
                paidDays = expectedWorkingDays;

            decimal lopDays = Math.Max(0, expectedWorkingDays - paidDays);

            return (paidDays, fullDays, halfDays, approvedLeaveDays, lopDays);
        }

        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Index(int? month, int? year, int? employeeId)
        {
            int m = month ?? DateTime.Today.Month;
            int y = year ?? DateTime.Today.Year;

            var query = _context.Salaries.Include(s => s.Employee).ThenInclude(e => e!.Department).Where(s => s.Month == m && s.Year == y).AsQueryable();

            if (employeeId.HasValue)
                query = query.Where(s => s.EmployeeId == employeeId.Value);

            var records = await query.OrderBy(s => s.Employee!.FullName).ToListAsync();

            ViewBag.Employees = await _context.Employees.Where(e => e.IsActive).OrderBy(e => e.FullName).ToListAsync();

            ViewBag.Month = m;
            ViewBag.Year = y;
            ViewBag.EmployeeId = employeeId;
            ViewBag.TotalDays = GetTotalDaysInMonth(y, m);
            ViewBag.Sundays = GetSundaysInMonth(y, m);
            ViewBag.WorkingDays = GetExpectedWorkingDays(y, m);
            ViewBag.TotalBasic = records.Sum(s => s.BasicPay);
            ViewBag.TotalNet = records.Sum(s => s.NetSalary);
            ViewBag.TotalDeduct = records.Sum(s => s.Deductions);

            return View(records);
        }

        [Authorize(Roles = "Admin,HR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateBulk(int month, int year)
        {
            int totalDays = GetTotalDaysInMonth(year, month);
            int sundays = GetSundaysInMonth(year, month);
            int workingDays = GetExpectedWorkingDays(year, month);

            var employees = await _context.Employees.Where(e => e.IsActive && e.Salary > 0).ToListAsync();

            int created = 0, updated = 0, skipped = 0;

            foreach (var emp in employees)
            {
                var (paidDays, presentFull, presentHalf,
                     approvedLeave, lopDays) =
                    await GetAttendanceSummary(emp.EmployeeId, month, year);

                // Per day based on TOTAL calendar days (31/30/28)
                decimal perDay = emp.Salary / totalDays;
                decimal lopAmount = Math.Round(perDay * lopDays, 2);
                decimal netSalary = Math.Round(emp.Salary - lopAmount, 2);

                var existing = await _context.Salaries.FirstOrDefaultAsync(s => s.EmployeeId == emp.EmployeeId && s.Month == month && s.Year == year);

                if (existing == null)
                {
                    _context.Salaries.Add(new Salary
                    {
                        EmployeeId = emp.EmployeeId,
                        BasicPay = emp.Salary,
                        Allowances = 0,
                        Deductions = lopAmount,
                        NetSalary = netSalary,
                        TotalDays = totalDays,
                        Sundays = sundays,
                        WorkingDays = workingDays,
                        PresentDays = presentFull,
                        HalfDays = presentHalf,
                        ApprovedLeave = approvedLeave,
                        LopDaysExact = lopDays,
                        Month = month,
                        Year = year,
                        IsFinalized = false
                    });
                    created++;
                }
                else if (!existing.IsFinalized)
                {
                    existing.BasicPay = emp.Salary;
                    existing.Deductions = lopAmount;
                    existing.NetSalary = Math.Round(emp.Salary + existing.Allowances - lopAmount, 2);
                    existing.TotalDays = totalDays;
                    existing.Sundays = sundays;
                    existing.WorkingDays = workingDays;
                    existing.PresentDays = presentFull;
                    existing.HalfDays = presentHalf;
                    existing.ApprovedLeave = approvedLeave;
                    existing.LopDaysExact = lopDays;
                    updated++;
                }
                else
                {
                    skipped++; // finalized — don't touch
                }
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Payroll for {new DateTime(year, month, 1):MMMM yyyy} — " +
                $"Calendar days: {totalDays} | Sundays (paid off): {sundays} | " +
                $"Working days: {workingDays} | " +
                $"Created: {created} | Recalculated: {updated} | " +
                $"Skipped (finalized): {skipped}.";

            return RedirectToAction(nameof(Index), new { month, year });
        }

        [Authorize(Roles = "Admin,HR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Finalize(int id)
        {
            var salary = await _context.Salaries.Include(s => s.Employee).FirstOrDefaultAsync(s => s.SalaryId == id);

            if (salary == null) return NotFound();

            salary.IsFinalized = true;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Salary for {salary.Employee?.FullName} — {new DateTime(salary.Year, salary.Month, 1):MMMM yyyy} has been finalized and locked.";

            return RedirectToAction(nameof(Index),
                new { month = salary.Month, year = salary.Year });
        }

        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> Upsert(
            int? id, int? employeeId, int? month, int? year)
        {
            ViewBag.Employees = await _context.Employees.Where(e => e.IsActive).OrderBy(e => e.FullName).ToListAsync();

            if (id.HasValue)
            {
                var existing = await _context.Salaries
                    .Include(s => s.Employee)
                    .FirstOrDefaultAsync(s => s.SalaryId == id.Value);

                if (existing == null) return NotFound();

                // Only Admin can edit a finalized record
                if (existing.IsFinalized && !User.IsInRole("Admin"))
                {
                    TempData["Error"] = "This salary is finalized. Only Admin can modify it.";
                    return RedirectToAction(nameof(Index),
                        new { month = existing.Month, year = existing.Year });
                }

                return View(existing);
            }

            int m = month ?? DateTime.Today.Month;
            int y = year ?? DateTime.Today.Year;

            var salary = new Salary
            {
                EmployeeId = employeeId ?? 0,
                Month = m,
                Year = y,
                TotalDays = GetTotalDaysInMonth(y, m),
                Sundays = GetSundaysInMonth(y, m),
                WorkingDays = GetExpectedWorkingDays(y, m)
            };

            return View(salary);
        }

        [Authorize(Roles = "Admin,HR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upsert(Salary salary)
        {
            // Always recompute NetSalary from HR-entered values
            salary.NetSalary = salary.BasicPay + salary.Allowances - salary.Deductions;

            // Recompute calendar info for that month
            salary.TotalDays = GetTotalDaysInMonth(salary.Year, salary.Month);
            salary.Sundays = GetSundaysInMonth(salary.Year, salary.Month);
            salary.WorkingDays = GetExpectedWorkingDays(salary.Year, salary.Month);

            if (!ModelState.IsValid)
            {
                ViewBag.Employees = await _context.Employees.Where(e => e.IsActive).OrderBy(e => e.FullName).ToListAsync();
                return View(salary);
            }

            // Duplicate check
            var duplicate = await _context.Salaries.AnyAsync(s => s.EmployeeId == salary.EmployeeId &&
                                                                  s.Month == salary.Month &&
                                                                  s.Year == salary.Year &&
                                                                  s.SalaryId != salary.SalaryId);

            if (duplicate)
            {
                ModelState.AddModelError("","A salary record already exists for this employee for the selected month. Edit the existing record instead.");
                ViewBag.Employees = await _context.Employees.Where(e => e.IsActive).OrderBy(e => e.FullName).ToListAsync();
                return View(salary);
            }

            if (salary.SalaryId == 0)
                _context.Salaries.Add(salary);
            else
                _context.Salaries.Update(salary);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Salary record saved successfully.";
            return RedirectToAction(nameof(Index),
                new { month = salary.Month, year = salary.Year });
        }

        [Authorize(Roles = "Admin,HR")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var salary = await _context.Salaries.Include(s => s.Employee).FirstOrDefaultAsync(s => s.SalaryId == id);

            if (salary == null) return NotFound();

            if (salary.IsFinalized && !User.IsInRole("Admin"))
            {
                TempData["Error"] = "Finalized records can only be deleted by Admin.";
                return RedirectToAction(nameof(Index),
                    new { month = salary.Month, year = salary.Year });
            }

            _context.Salaries.Remove(salary);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Salary record for {salary.Employee?.FullName} deleted.";
            return RedirectToAction(nameof(Index),
                new { month = salary.Month, year = salary.Year });
        }

        [Authorize(Roles = "Employee,HR,Manager")]
        public async Task<IActionResult> MySlips(int? year)
        {
            var userId = _userManager.GetUserId(User);

            var employee = await _context.Employees.Include(e => e.Department).FirstOrDefaultAsync(e => e.UserId == userId);

            if (employee == null) return Unauthorized();

            int y = year ?? DateTime.Today.Year;

            var slips = await _context.Salaries.Where(s => s.EmployeeId == employee.EmployeeId && s.Year == y).OrderByDescending(s => s.Month).ToListAsync();

            var availableYears = await _context.Salaries.Where(s => s.EmployeeId == employee.EmployeeId).Select(s => s.Year).Distinct().OrderByDescending(yr => yr).ToListAsync();

            if (!availableYears.Contains(DateTime.Today.Year))
                availableYears.Insert(0, DateTime.Today.Year);

            ViewBag.Year = y;
            ViewBag.Employee = employee;
            ViewBag.AvailableYears = availableYears;

            return View(slips);
        }

        [Authorize(Roles = "Admin,HR,Employee,Manager")]
        public async Task<IActionResult> SlipDetail(int id)
        {
            var salary = await _context.Salaries.Include(s => s.Employee).ThenInclude(e => e!.Department).Include(s => s.Employee).ThenInclude(e => e!.Designation).FirstOrDefaultAsync(s => s.SalaryId == id);

            if (salary == null) return NotFound();

            // Employee can only see their own slip
            if (User.IsInRole("Employee") || User.IsInRole("Manager"))
            {
                var userId = _userManager.GetUserId(User);
                var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);

                if (employee == null || salary.EmployeeId != employee.EmployeeId)
                    return Forbid();
            }

            return View(salary);
        }
    }
}