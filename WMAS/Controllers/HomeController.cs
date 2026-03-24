using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using WMAS.Data;
using WMAS.Models;

namespace WMAS.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public HomeController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            // Not logged in Public home
            if (!User.Identity!.IsAuthenticated)
            {
                return View("Public");
            }

            // Admin dashboard
            if (User.IsInRole("Admin"))
            {
                ViewBag.TotalEmployees = await _context.Employees.CountAsync();
                ViewBag.ActiveEmployees = await _context.Employees.CountAsync(e => e.IsActive);
                ViewBag.PendingLeaves = await _context.Leaves.CountAsync(l => l.Status == "Pending");
                ViewBag.TodayPresent = await _context.Attendances.CountAsync(a => a.Date == DateTime.Today && a.Status == "Present");

                return View("AdminDashboard");
            }

            // Employee dashboard
            if (User.IsInRole("Employee"))
            {
                var userId = _userManager.GetUserId(User);

                var employee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.UserId == userId);

                if (employee == null)
                    return Unauthorized();

                var today = DateTime.Today;

                var attendance = await _context.Attendances.FirstOrDefaultAsync(a => a.EmployeeId == employee.EmployeeId && a.Date == today);
                var notes = await _context.Notes.Where(n => n.UserId == userId).OrderByDescending(n => n.CreatedOn).ToListAsync();

                ViewBag.ShowPasswordWarning = employee != null && !employee.IsPasswordChanged;
                ViewBag.Attendance = attendance;
                ViewBag.Notes = notes;

                return View("EmployeeDashboard");
            }

            // fallback
            return View("Public");
        }

        // ---------------- EMPLOYEE NOTES ----------------

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

        // ---------------- DEFAULT MVC ----------------

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
