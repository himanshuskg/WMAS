using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMAS.Data;
using WMAS.Models;

namespace WMAS.Controllers
{
    [Authorize(Roles = "Employee")]
    public class AttendanceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public AttendanceController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // CHECK IN
        [HttpPost]
        public async Task<IActionResult> CheckIn()
        {
            var userId = _userManager.GetUserId(User);

            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.UserId == userId);

            if (employee == null)
                return Unauthorized();

            var today = DateTime.Today;

            var attendance = await _context.Attendances
                .FirstOrDefaultAsync(a =>
                    a.EmployeeId == employee.EmployeeId &&
                    a.Date == today);

            if (attendance != null)
            {
                TempData["Error"] = "You have already checked in today.";
                return RedirectToAction("Index", "Home");
            }

            attendance = new Attendance
            {
                EmployeeId = employee.EmployeeId,
                Date = today,
                CheckInTime = DateTime.Now,
                Status = "Present"
            };

            _context.Attendances.Add(attendance);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Checked in successfully.";
            return RedirectToAction("Index", "Home");
        }

        // CHECK OUT
        [HttpPost]
        public async Task<IActionResult> CheckOut()
        {
            var userId = _userManager.GetUserId(User);

            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.UserId == userId);

            if (employee == null)
                return Unauthorized();

            var today = DateTime.Today;

            var attendance = await _context.Attendances
                .FirstOrDefaultAsync(a =>
                    a.EmployeeId == employee.EmployeeId &&
                    a.Date == today);

            if (attendance == null || attendance.CheckInTime == null)
            {
                TempData["Error"] = "You must check in first.";
                return RedirectToAction("Index", "Home");
            }

            if (attendance.CheckOutTime != null)
            {
                TempData["Error"] = "You have already checked out.";
                return RedirectToAction("Index", "Home");
            }

            attendance.CheckOutTime = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Checked out successfully.";
            return RedirectToAction("Index", "Home");
        }
    }
}
