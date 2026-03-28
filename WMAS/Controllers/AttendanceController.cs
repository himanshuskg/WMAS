using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMAS.Data;
using WMAS.Models;

namespace WMAS.Controllers
{
    public class AttendanceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public AttendanceController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpPost]
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> CheckIn()
        {
            var userId = _userManager.GetUserId(User);
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);

            if (employee == null) return Unauthorized();
            if (!employee.IsActive)
            {
                TempData["Error"] = "Your account is inactive.";
                return RedirectToAction("Index", "Home");
            }

           
            var exists = await _context.Attendances.AnyAsync(a => a.EmployeeId == employee.EmployeeId && a.Date == DateTime.Now);

            if (exists)
            {
                TempData["Error"] = "Already checked in today.";
                return RedirectToAction("Index", "Home");
            }

            _context.Attendances.Add(new Attendance
            {
                EmployeeId = employee.EmployeeId,
                Date = DateTime.Now,
                CheckInTime = DateTime.Now,
                Status = "Present"
            });
            await _context.SaveChangesAsync();

            TempData["Success"] = "Checked in successfully.";
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> CheckOut()
        {
            var userId = _userManager.GetUserId(User);
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);

            if (employee == null) return Unauthorized();
            if (!employee.IsActive)
            {
                TempData["Error"] = "Your account is inactive.";
                return RedirectToAction("Index", "Home");
            }
            var today = DateTime.Today;
            var attendance = await _context.Attendances.Where(a =>a.EmployeeId == employee.EmployeeId && a.CheckInTime != null && a.CheckOutTime == null)
                                                       .OrderByDescending(a => a.CheckInTime).FirstOrDefaultAsync();
            if (attendance == null || attendance.CheckInTime == null)
            {
                TempData["Error"] = "You must check in first.";
                return RedirectToAction("Index", "Home");
            }
            if (attendance.CheckOutTime != null)
            {
                TempData["Error"] = "Already checked out today.";
                return RedirectToAction("Index", "Home");
            }

            attendance.CheckOutTime = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Checked out successfully.";
            return RedirectToAction("Index", "Home");
        }

        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> MyAttendance(int? month, int? year)
        {
            var userId = _userManager.GetUserId(User);
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);
            if (employee == null) return Unauthorized();

            int m = month ?? DateTime.Today.Month;
            int y = year ?? DateTime.Today.Year;

            var records = await _context.Attendances.Where(a => a.EmployeeId == employee.EmployeeId && a.Date.Month == m && a.Date.Year == y)
                                                    .OrderByDescending(a => a.Date).ToListAsync();

            ViewBag.Month = m;
            ViewBag.Year = y;
            ViewBag.Present = records.Count(a => a.Status == "Present");
            ViewBag.Absent = records.Count(a => a.Status == "Absent");
            ViewBag.Leave = records.Count(a => a.Status == "Leave");

            return View(records);
        }


        [Authorize(Roles = "Admin,HR")]
        public async Task<IActionResult> AdminIndex(int? employeeId, int? month, int? year)
        {
            int m = month ?? DateTime.Today.Month;
            int y = year ?? DateTime.Today.Year;

            var query = _context.Attendances.Include(a => a.Employee).Where(a => a.Date.Month == m && a.Date.Year == y);

            if (employeeId.HasValue)
                query = query.Where(a => a.EmployeeId == employeeId.Value);

            var records = await query.OrderBy(a => a.Employee.FullName)
                                     .ThenByDescending(a => a.Date)
                                     .ToListAsync();

            ViewBag.Employees = await _context.Employees.Where(e => e.IsActive).OrderBy(e => e.FullName).ToListAsync();

            ViewBag.Month = m;
            ViewBag.Year = y;
            ViewBag.EmployeeId = employeeId;

            return View(records);
        }

        // ── MANAGER: Team attendance only ─────────────────────
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> ManagerIndex(DateOnly? date, int? employeeId)
        {
            var userId = _userManager.GetUserId(User);
            var manager = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);

            if (manager == null) return Unauthorized();

            // Get only direct subordinates
            var teamIds = await _context.Employees.Where(e => e.ReportingManagerId == manager.EmployeeId && e.IsActive).Select(e => e.EmployeeId).ToListAsync();

            var query = _context.Attendances.Include(a => a.Employee).Where(a => teamIds.Contains(a.EmployeeId)).AsQueryable();

            // Filters
            if (date.HasValue)
                query = query.Where(a => a.Date == Convert.ToDateTime(date.Value));

            if (employeeId.HasValue)
                query = query.Where(a => a.EmployeeId == employeeId.Value);

            var records = await query.OrderByDescending(a => a.Date).ThenBy(a => a.Employee!.FullName).ToListAsync();

            // Only team members in dropdown
            ViewBag.TeamMembers = await _context.Employees.Where(e => teamIds.Contains(e.EmployeeId)).OrderBy(e => e.FullName).ToListAsync();

            ViewBag.SelectedDate = date;
            ViewBag.SelectedEmployeeId = employeeId;
            ViewBag.ManagerName = manager.FullName;

            return View("ManagerIndex", records);
        }
    }
}
