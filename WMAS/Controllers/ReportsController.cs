using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMAS.Data;

namespace WMAS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ReportController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int? month, int? year)
        {
            int m = month ?? DateTime.Today.Month;
            int y = year ?? DateTime.Today.Year;

            // Attendance summary
            var attendance = await _context.Attendances
                .Include(a => a.Employee)
                .Where(a => a.Date.Month == m && a.Date.Year == y)
                .ToListAsync();

            // Leave summary
            var leaves = await _context.Leaves
                .Include(l => l.Employee)
                .Where(l => l.FromDate.Month == m && l.FromDate.Year == y)
                .ToListAsync();

            // Payroll summary
            var salaries = await _context.Salaries
                .Include(s => s.Employee)
                .Where(s => s.Month == m && s.Year == y)
                .ToListAsync();

            // Employee summary
            var totalEmployees = await _context.Employees.CountAsync();
            var activeEmployees = await _context.Employees.CountAsync(e => e.IsActive);

            ViewBag.Month = m;
            ViewBag.Year = y;
            ViewBag.TotalEmployees = totalEmployees;
            ViewBag.ActiveEmployees = activeEmployees;

            // Attendance stats
            ViewBag.TotalPresent = attendance.Count(a => a.Status == "Present");
            ViewBag.TotalAbsent = attendance.Count(a => a.Status == "Absent");
            ViewBag.AttendanceRecords = attendance;

            // Leave stats
            ViewBag.TotalLeaves = leaves.Count;
            ViewBag.ApprovedLeaves = leaves.Count(l => l.Status == "Approved");
            ViewBag.PendingLeaves = leaves.Count(l => l.Status == "Pending");
            ViewBag.RejectedLeaves = leaves.Count(l => l.Status == "Rejected");

            // Payroll stats
            ViewBag.TotalPayroll = salaries.Sum(s => s.NetSalary);
            ViewBag.PayrollCount = salaries.Count;

            return View();
        }
    }
}
