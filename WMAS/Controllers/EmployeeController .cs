using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WMAS.Data;
using WMAS.Models;

namespace WMAS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class EmployeeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public EmployeeController(ApplicationDbContext context,
                                  UserManager<IdentityUser> userManager,
                                  RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // -------------------- LIST --------------------
        public async Task<IActionResult> Index()
        {
            var employees = await _context.Employees.Include(e => e.Department).Include(e => e.Designation).ToListAsync();
            return View(employees);
        }

        // -------------------- DETAILS --------------------
        public async Task<IActionResult> Details(int id)
        {
            var employee = await _context.Employees.Include(e => e.Department).Include(e => e.Designation)
                                                   .FirstOrDefaultAsync(e => e.EmployeeId == id);

            if (employee == null)
                return NotFound();

            return View(employee);
        }

        // -------------------- CREATE --------------------
        public IActionResult Create()
        {
            LoadDropdowns();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Employee employee)
        {
            if (!ModelState.IsValid)
            {
                LoadDropdowns();
                return View(employee);
            }

            if (employee.HasSystemAccess)
            {
                var result = await CreateEmployeeUserAsync(employee);
                if (!result.Success)
                {
                    ModelState.AddModelError("", "Failed to create system user.");
                    LoadDropdowns();
                    return View(employee);
                }

                TempData["Success"] = $"Employee created. Temporary password: {result.Password}";
            }

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // -------------------- EDIT --------------------
        public async Task<IActionResult> Edit(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
                return NotFound();

            LoadDropdowns();
            return View(employee);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Employee model)
        {
            var employee = await _context.Employees.FindAsync(model.EmployeeId);
            if (employee == null)
                return NotFound();

            if (!ModelState.IsValid)
            {
                LoadDropdowns();
                return View(model);
            }

            // Grant access later
            if (!employee.HasSystemAccess && model.HasSystemAccess)
            {
                var result = await CreateEmployeeUserAsync(employee);
                if (!result.Success)
                {
                    ModelState.AddModelError("", "Failed to grant system access.");
                    LoadDropdowns();
                    return View(model);
                }

                TempData["Success"] = $"System access granted. Temporary password: {result.Password}";
            }

            employee.FullName = model.FullName;
            employee.Email = model.Email;
            employee.Phone = model.Phone;
            employee.DepartmentId = model.DepartmentId;
            employee.DesignationId = model.DesignationId;
            employee.IsActive = model.IsActive;

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // -------------------- RESET PASSWORD --------------------
        [HttpPost]
        public async Task<IActionResult> ResetPassword(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null || employee.UserId == null)
                return NotFound();

            var user = await _userManager.FindByIdAsync(employee.UserId);

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var newPassword = GeneratePassword();

            await _userManager.ResetPasswordAsync(user, token, newPassword);

            TempData["Success"] = $"Password reset successfully. Temporary password: {newPassword}";

            return RedirectToAction(nameof(Details), new { id });
        }

        #region Private Helpers
        private async Task<(bool Success, string? Password)> CreateEmployeeUserAsync(Employee employee)
        {
            var user = new IdentityUser
            {
                UserName = employee.Email,
                Email = employee.Email,
                EmailConfirmed = true
            };

            var password = GeneratePassword();
            var result = await _userManager.CreateAsync(user, password);

            if (!result.Succeeded)
                return (false, null);

            await _userManager.AddToRoleAsync(user, "Employee");

            employee.UserId = user.Id;
            employee.HasSystemAccess = true;

            return (true, password);
        }

        private string GeneratePassword()
        {
            return $"Emp@{Guid.NewGuid().ToString("N")[..8]}";
        }

        private void LoadDropdowns()
        {
            ViewBag.Departments = _context.Departments.Select(d => new SelectListItem
                                                      {
                                                          Value = d.DepartmentId.ToString(),
                                                          Text = d.DepartmentName
                                                      }).ToList();

            ViewBag.Designations = _context.Designations.Select(d => new SelectListItem
                                                        {
                                                            Value = d.DesignationId.ToString(),
                                                            Text = d.Title
                                                        }).ToList();
        }
        #endregion
    }
}