using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMAS.Data;
using WMAS.Models;

namespace WMAS.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public HomeController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            if (!User.Identity!.IsAuthenticated)
                return View("Public");

            if (User.IsInRole("Admin"))
                return RedirectToAction("AdminDashboard");

            if (User.IsInRole("HR"))
                return RedirectToAction("HRDashboard");

            if (User.IsInRole("Manager"))
                return RedirectToAction("ManagerDashboard");

            return RedirectToAction("EmployeeDashboard");
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminDashboard()
        {
            ViewBag.TotalEmployees = await _context.Employees.CountAsync();
            ViewBag.ActiveEmployees = await _context.Employees.CountAsync(e => e.IsActive);
            ViewBag.PendingLeaves = await _context.Leaves.CountAsync(l => l.Status == "Pending");
            ViewBag.TodayPresent = await _context.Attendances.CountAsync(a => a.Date == DateTime.Today && a.Status == "Present");

            return View();
        }

        [Authorize(Roles = "HR")]
        public async Task<IActionResult> HRDashboard()
        {
            await SetAttendanceStatus();
            ViewBag.TotalEmployees = await _context.Employees.CountAsync();
            ViewBag.ActiveEmployees = await _context.Employees.CountAsync(e => e.IsActive);
            ViewBag.PendingLeaves = await _context.Leaves.CountAsync(l => l.Status == "Pending");
            ViewBag.TodayPresent = await _context.Attendances.CountAsync(a => a.Date == DateTime.Today && a.Status == "Present");

            // Salary stats for current month
            int m = DateTime.Today.Month, y = DateTime.Today.Year;
            ViewBag.SalariesGenerated = await _context.Salaries.CountAsync(s => s.Month == m && s.Year == y);
            ViewBag.SalariesFinalized = await _context.Salaries.CountAsync(s => s.Month == m && s.Year == y && s.IsFinalized);

            return View();
        }

        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> ManagerDashboard()
        {
            await SetAttendanceStatus();
            var userId = _userManager.GetUserId(User);
            var manager = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);

            if (manager == null) return Unauthorized();

            var teamIds = await _context.Employees.Where(e => e.ReportingManagerId == manager.EmployeeId).Select(e => e.EmployeeId).ToListAsync();

            ViewBag.TeamCount = teamIds.Count;
            ViewBag.TeamPresent = await _context.Attendances.CountAsync(a => teamIds.Contains(a.EmployeeId) && a.Date == DateTime.Today && a.Status == "Present");    
            ViewBag.TeamOnLeave = await _context.Leaves.CountAsync(l => teamIds.Contains(l.EmployeeId) && l.Status == "Approved" && l.FromDate <= DateTime.Today && l.ToDate >= DateTime.Today);
            ViewBag.PendingLeaves = await _context.Leaves.CountAsync(l => teamIds.Contains(l.EmployeeId) && l.Status == "Pending");
            ViewBag.ManagerName = manager.FullName;

            return View();
        }

        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> EmployeeDashboard()
        {
            await SetAttendanceStatus();
            var userId = _userManager.GetUserId(User);
            var employee = await _context.Employees.Include(e => e.Department).Include(e => e.Designation).FirstOrDefaultAsync(e => e.UserId == userId);

            if (employee == null) return Unauthorized();

            var todayAtt = await _context.Attendances.FirstOrDefaultAsync(a => a.EmployeeId == employee.EmployeeId && a.Date == DateTime.Today);
            var notes = await _context.Notes.Where(n => n.UserId == userId).OrderByDescending(n => n.CreatedOn).ToListAsync();

            int year = DateTime.Today.Year;
            int usedLeaves = await _context.Leaves.CountAsync(l => l.EmployeeId == employee.EmployeeId && l.Status == "Approved" && l.FromDate.Year == year);
            int pendingLeaves = await _context.Leaves.CountAsync(l => l.EmployeeId == employee.EmployeeId && l.Status == "Pending");
            var latestSalary = await _context.Salaries.Where(s => s.EmployeeId == employee.EmployeeId).OrderByDescending(s => s.Year).ThenByDescending(s => s.Month).FirstOrDefaultAsync();

            ViewBag.Employee = employee;
            ViewBag.Notes = notes;
            ViewBag.TodayAtt = todayAtt;
            ViewBag.UsedLeaves = usedLeaves;
            ViewBag.PendingLeaves = pendingLeaves;
            ViewBag.LatestSalary = latestSalary;

            return View();
        }
        [HttpPost]
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> AddNote(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return RedirectToAction(nameof(Index));

            var userId = _userManager.GetUserId(User);

            var note = new Note
            {
                UserId = userId,
                Content = content
            };

            _context.Notes.Add(note);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> DeleteNote(int id)
        {
            var userId = _userManager.GetUserId(User);

            var note = await _context.Notes.FirstOrDefaultAsync(n => n.NoteId == id && n.UserId == userId);

            if (note != null)
            {
                _context.Notes.Remove(note);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }
        public IActionResult Privacy() => View();
        private async Task SetAttendanceStatus()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                ViewBag.TodayAtt = null;
                return;
            }
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);
            if (employee == null)
            {
                ViewBag.TodayAtt = null;
                return;
            }
            var todayAtt = await _context.Attendances.FirstOrDefaultAsync(a =>a.EmployeeId == employee.EmployeeId && a.Date == DateTime.Today);
            ViewBag.TodayAtt = todayAtt;
        }
    }
}