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
        public async Task<IActionResult> Create()
        {
            await LoadDropdownsAsync();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Employee employee)
        {
            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync();
                return View(employee);
            }

            // FIRST save employee
            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            // THEN create system access if needed
            if (employee.HasSystemAccess)
            {
                var result = await CreateOrResetUserAsync(employee);

                if (!result.success)
                {
                    ModelState.AddModelError("", "Failed to create system user.");
                    await LoadDropdownsAsync();
                    return View(employee);
                }

                TempData["Success"] = $"Employee created. Temporary password: {result.password}";
                await _context.SaveChangesAsync(); // save UserId + password info
            }
            return RedirectToAction(nameof(Index));
        }


        // -------------------- EDIT --------------------
        public async Task<IActionResult> Edit(int id)
        {
            var employee = await _context.Employees.Include(e => e.Department)
                                                   .Include(e => e.Designation)
                                                   .FirstOrDefaultAsync(e => e.EmployeeId == id);

            if (employee == null)
                return NotFound();

            await LoadDropdownsAsync(employee.DepartmentId, employee.DesignationId);
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
                await LoadDropdownsAsync(employee.DepartmentId, employee.DesignationId);
                return View(model);
            }

            // Update basic fields FIRST
            employee.FullName = model.FullName;
            employee.Email = model.Email;
            employee.Phone = model.Phone;
            employee.DepartmentId = model.DepartmentId;
            employee.DesignationId = model.DesignationId;
            employee.IsActive = model.IsActive;

            // Grant system access if newly enabled
            if (!employee.HasSystemAccess && model.HasSystemAccess)
            {
                var result = await CreateOrResetUserAsync(employee);
                if (!result.success)
                {
                    ModelState.AddModelError("", "Failed to grant system access.");
                    await LoadDropdownsAsync(employee.DepartmentId, employee.DesignationId);
                    return View(model);
                }
                employee.HasSystemAccess = true;
                TempData["Success"] = $"System access granted. Temporary password: {result.password}";
            }
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // -------------------- RESET PASSWORD --------------------
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ResetPassword(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null || string.IsNullOrEmpty(employee.UserId))
                return NotFound();

            var user = await _userManager.FindByIdAsync(employee.UserId);
            if (user == null)
                return NotFound();

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var newPassword = GeneratePassword();

            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

            if (!result.Succeeded)
            {
                TempData["Error"] = "Password reset failed.";
                return RedirectToAction(nameof(Details), new { id });
            }

            //TRACK RESET
            employee.GeneratedPassword = newPassword;
            employee.IsPasswordChanged = false;
            employee.PasswordResetCount++;

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Password reset successfully. Temporary password: {newPassword}";

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null) return NotFound();

            employee.IsActive = false;
            employee.DeactivatedOn = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Employee deactivated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
                return NotFound();

            employee.IsActive = true;
            employee.DeactivatedOn = null;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Employee activated successfully.";
            return RedirectToAction(nameof(Index));
        }


        #region Private Helpers

        private async Task<(bool success, string? password)> CreateOrResetUserAsync(Employee employee, bool isReset = false)
        {
            IdentityUser user;

            if (string.IsNullOrEmpty(employee.UserId))
            {
                // CREATE USER
                user = new IdentityUser
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
                employee.GeneratedPassword = password;
                employee.IsPasswordChanged = false;
                employee.PasswordResetCount = 0;

                return (true, password);
            }
            else
            {
                // RESET PASSWORD
                user = await _userManager.FindByIdAsync(employee.UserId);
                if (user == null) return (false, null);

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var password = GeneratePassword();

                var result = await _userManager.ResetPasswordAsync(user, token, password);
                if (!result.Succeeded)
                    return (false, null);

                employee.GeneratedPassword = password;
                employee.IsPasswordChanged = false;
                employee.PasswordResetCount++;

                return (true, password);
            }
        }

        private string GeneratePassword()
        {
            return $"Emp@{Guid.NewGuid().ToString("N")[..8]}";
        }

        private async Task LoadDropdownsAsync(int? departmentId = null, int? designationId = null)
        {
            ViewBag.Departments = await _context.Departments.Where(d => d.IsActive || d.DepartmentId == departmentId)
                                                            .Select(d => new SelectListItem
                                                            {
                                                                Value = d.DepartmentId.ToString(),
                                                                Text = d.IsActive ? d.DepartmentName : $"{d.DepartmentName} (Inactive)",
                                                                Disabled = !d.IsActive && d.DepartmentId != departmentId,
                                                                Selected = d.DepartmentId == departmentId
                                                            }).ToListAsync();

            ViewBag.Designations = await _context.Designations.Where(d => d.IsActive || d.DesignationId == designationId)
                                                              .Select(d => new SelectListItem
                                                              {
                                                                  Value = d.DesignationId.ToString(),
                                                                  Text = d.IsActive ? d.Title : $"{d.Title} (Inactive)",
                                                                  Disabled = !d.IsActive && d.DesignationId != designationId,
                                                                  Selected = d.DesignationId == designationId
                                                              }).ToListAsync();
        }

        #endregion
    }
}