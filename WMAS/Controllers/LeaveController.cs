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

        public LeaveController(ApplicationDbContext context,UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);

            var leaves = await _context.Leaves.Where(l => l.EmployeeId == employee.EmployeeId)
                                              .OrderByDescending(l => l.CreatedOn).ToListAsync();

            return View(leaves);
        }

        public IActionResult Apply()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apply(Leave leave)
        {
            if (!ModelState.IsValid)
                return View(leave);

            var userId = _userManager.GetUserId(User);
            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);
            if (!employee.IsActive)
            {
                TempData["Error"] = "Your account is inactive.";
                return RedirectToAction("Index", "Home");
            }
            leave.EmployeeId = employee.EmployeeId;
            leave.Status = "Pending";

            _context.Leaves.Add(leave);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}
