using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMAS.Data;
using WMAS.Models;

namespace WMAS.Controllers
{
    [Authorize(Roles = "Employee")]
    public class LeaveController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public LeaveController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);
            if (employee == null) return Unauthorized();

            var leaves = await _context.Leaves.Where(l => l.EmployeeId == employee.EmployeeId).OrderByDescending(l => l.CreatedOn).ToListAsync();
            int currentYear = DateTime.Today.Year;
            int totalAllowed = 14;
            int usedThisYear = leaves.Where(l => l.Status == "Approved" && l.FromDate.Year == currentYear)
                                     .Sum(l => (l.ToDate - l.FromDate).Days + 1);
            int pending = leaves.Count(l => l.Status == "Pending");

            ViewBag.TotalAllowed = totalAllowed;
            ViewBag.UsedLeaves = usedThisYear;
            ViewBag.Remaining = Math.Max(0, totalAllowed - usedThisYear);
            ViewBag.Pending = pending;

            return View(leaves.OrderByDescending(l => l.FromDate).ToList());
        }

        // Apply GET
        public IActionResult Apply() => View();

        // Apply POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apply(Leave leave)
        {
            if (leave.FromDate.Date < DateTime.Today)
                ModelState.AddModelError("FromDate", "From date cannot be in the past.");

            if (leave.ToDate < leave.FromDate)
                ModelState.AddModelError("ToDate", "To date cannot be before From date.");

            if (!ModelState.IsValid)
                return View(leave);

            var userId = _userManager.GetUserId(User);
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);
            if (employee == null) return Unauthorized();

            if (!employee.IsActive)
            {
                TempData["Error"] = "Your account is inactive.";
                return RedirectToAction("Index", "Home");
            }

            // Check overlapping leave
            var overlap = await _context.Leaves.AnyAsync(l => l.EmployeeId == employee.EmployeeId &&
                                                              l.Status != "Rejected" &&
                                                              l.FromDate <= leave.ToDate &&
                                                              l.ToDate >= leave.FromDate);

            if (overlap)
            {
                ModelState.AddModelError("", "You already have a leave in this date range.");
                return View(leave);
            }

            leave.EmployeeId = employee.EmployeeId;
            leave.Status = "Pending";

            _context.Leaves.Add(leave);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Leave applied successfully.";
            return RedirectToAction(nameof(Index));
        }

        // Cancel leave (only if Pending)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var userId = _userManager.GetUserId(User);
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);
            if (employee == null) return Unauthorized();

            var leave = await _context.Leaves
                .FirstOrDefaultAsync(l => l.LeaveId == id && l.EmployeeId == employee.EmployeeId);

            if (leave == null || leave.Status != "Pending")
            {
                TempData["Error"] = "Only pending leaves can be cancelled.";
                return RedirectToAction(nameof(Index));
            }

            _context.Leaves.Remove(leave);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Leave cancelled.";
            return RedirectToAction(nameof(Index));
        }
    }
}
