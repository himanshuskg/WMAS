using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMAS.Data;
using WMAS.Models;

namespace WMAS.Controllers
{
    [Authorize]
    public class LeaveApprovalController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public LeaveApprovalController(ApplicationDbContext context,UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [Authorize(Roles = "Admin,HR,Manager")]
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            if (User.IsInRole("Admin") || User.IsInRole("HR"))
            {
                var all = await _context.Leaves.Include(l => l.Employee).ThenInclude(e => e!.Department)
                                            .Where(l => l.Status == "Pending")
                                            .OrderByDescending(l => l.CreatedOn)
                                            .ToListAsync();

                ViewBag.IsAdminOrHR = true;
                return View(all);
            }

            if (User.IsInRole("Manager"))
            {
                var manager = await _context.Employees
                    .FirstOrDefaultAsync(e => e.UserId == userId);

                if (manager == null) return Unauthorized();

                var team = await _context.Leaves.Include(l => l.Employee).ThenInclude(e => e!.Department)
                                                .Where(l => l.Status == "Pending" && l.Employee!.ReportingManagerId == manager.EmployeeId)
                                                .OrderByDescending(l => l.CreatedOn).ToListAsync();

                ViewBag.IsAdminOrHR = false;
                return View(team);
            }

            return Forbid();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,HR,Manager")]
        public async Task<IActionResult> Approve(int id, string? actionComments)
        {
            var leave = await _context.Leaves.Include(l => l.Employee).FirstOrDefaultAsync(l => l.LeaveId == id && l.Status == "Pending");

            if (leave == null)
            {
                TempData["Error"] = "Leave not found or already actioned.";
                return RedirectToAction(nameof(Index));
            }

            var userId = _userManager.GetUserId(User);

            if (User.IsInRole("Admin") || User.IsInRole("HR"))
            {
                leave.Status = "Approved";
                leave.ActionOn = DateTime.Now;
                leave.ActionComments = actionComments;
            }
            else if (User.IsInRole("Manager"))
            {
                var manager = await _context.Employees
                    .FirstOrDefaultAsync(e => e.UserId == userId);

                if (manager == null ||
                    leave.Employee!.ReportingManagerId != manager.EmployeeId)
                    return Forbid();

                leave.Status = "Approved";
                leave.ActionById = manager.EmployeeId;
                leave.ActionOn = DateTime.Now;
                leave.ActionComments = actionComments;
            }
            else return Forbid();

            await _context.SaveChangesAsync();
            TempData["Success"] =
                $"Leave approved for {leave.Employee!.FullName}.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,HR,Manager")]
        public async Task<IActionResult> Reject(int id, string? actionComments)
        {
            var leave = await _context.Leaves.Include(l => l.Employee).FirstOrDefaultAsync(l => l.LeaveId == id && l.Status == "Pending");

            if (leave == null)
            {
                TempData["Error"] = "Leave not found or already actioned.";
                return RedirectToAction(nameof(Index));
            }

            var userId = _userManager.GetUserId(User);

            if (User.IsInRole("Admin") || User.IsInRole("HR"))
            {
                leave.Status = "Rejected";
                leave.ActionOn = DateTime.Now;
                leave.ActionComments = actionComments;
            }
           
            else if (User.IsInRole("Manager"))
            {
                var manager = await _context.Employees
                    .FirstOrDefaultAsync(e => e.UserId == userId);

                if (manager == null || leave.Employee!.ReportingManagerId != manager.EmployeeId)
                    return Forbid();

                leave.Status = "Rejected";
                leave.ActionById = manager.EmployeeId;
                leave.ActionOn = DateTime.Now;
                leave.ActionComments = actionComments;
            }
            else return Forbid();

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Leave rejected for {leave.Employee!.FullName}.";
            return RedirectToAction(nameof(Index));
        }
    }
}