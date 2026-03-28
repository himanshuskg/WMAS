using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WMAS.Data;
using WMAS.Models;
using WMAS.Models.ViewModels;

namespace WMAS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class RoleAdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        private static readonly string[] AllowedRoles = new[] { "HR", "Manager" };

        public RoleAdminController(ApplicationDbContext context, UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // Ensure HR/Manager roles exist
        private async Task EnsureRolesAsync()
        {
            foreach (var role in AllowedRoles)
            {
                if (!await _roleManager.RoleExistsAsync(role))
                    await _roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // GET: /RoleAdmin/Index?departmentId=&roleFilter=
        public async Task<IActionResult> Index(int? departmentId, string? roleFilter)
        {
            await EnsureRolesAsync();

            var employeesQuery = _context.Employees
                .Include(e => e.Department)
                .OrderBy(e => e.FullName)
                .AsQueryable();

            if (departmentId.HasValue)
                employeesQuery = employeesQuery.Where(e => e.DepartmentId == departmentId.Value);

            var employees = await employeesQuery.ToListAsync();

            var userIds = employees
                .Where(e => !string.IsNullOrEmpty(e.UserId))
                .Select(e => e.UserId!)
                .ToList();

            var users = await _userManager.Users
                .Where(u => userIds.Contains(u.Id))
                .ToListAsync();

            var model = new List<RoleAssignmentViewModel>();

            foreach (var emp in employees)
            {
                var vm = new RoleAssignmentViewModel
                {
                    EmployeeId = emp.EmployeeId,
                    FullName = emp.FullName,
                    Email = emp.Email,
                    Department = emp.Department?.DepartmentName,
                    IsActive = emp.IsActive,
                    CurrentRoles = new List<string>()
                };

                if (!string.IsNullOrEmpty(emp.UserId))
                {
                    var user = users.FirstOrDefault(u => u.Id == emp.UserId);
                    if (user != null)
                    {
                        var roles = await _userManager.GetRolesAsync(user);
                        vm.CurrentRoles = roles.Where(r => AllowedRoles.Contains(r)).ToList();
                    }
                }

                model.Add(vm);
            }

            if (!string.IsNullOrEmpty(roleFilter) && AllowedRoles.Contains(roleFilter))
                model = model.Where(m => m.CurrentRoles.Contains(roleFilter)).ToList();

            ViewBag.Departments = await _context.Departments
                .OrderBy(d => d.DepartmentName)
                .ToListAsync();

            ViewBag.DepartmentId = departmentId;
            ViewBag.RoleFilter = roleFilter;

            return View(model);
        }

        // POST: /RoleAdmin/UpdateRole
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRole(int employeeId, string role, bool? isAssigned,int? departmentId, string? roleFilter)
        {
            if (!AllowedRoles.Contains(role))
            {
                TempData["Error"] = "Invalid role.";
                return RedirectToAction(nameof(Index));
            }

            var employee = await _context.Employees
                .FirstOrDefaultAsync(e => e.EmployeeId == employeeId);

            if (employee == null || string.IsNullOrEmpty(employee.UserId))
            {
                TempData["Error"] = "Employee or user account not found.";
                return RedirectToAction(nameof(Index));
            }

            await EnsureRolesAsync();

            var user = await _userManager.FindByIdAsync(employee.UserId);
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction(nameof(Index));
            }
            if (isAssigned == null)
            {
                TempData["Error"] = "Toggle value missing.";
                int? deptF = (departmentId.HasValue && departmentId.Value != 0) ? departmentId : null;
                return RedirectToAction(nameof(Index), new { departmentId = deptF, roleFilter });
            }

            var hasRole = await _userManager.IsInRoleAsync(user, role);
            if (isAssigned.Value && !hasRole)
            {
                await _userManager.AddToRoleAsync(user, role);
                TempData["Success"] = $"Role '{role}' assigned to {employee.FullName}.";
            }
            else if (!isAssigned.Value && hasRole)
            {
                await _userManager.RemoveFromRoleAsync(user, role);
                TempData["Success"] = $"Role '{role}' removed from {employee.FullName}.";
            }
            int? deptFilter = (departmentId.HasValue && departmentId.Value != 0) ? departmentId : null;

            return RedirectToAction(nameof(Index), new { departmentId = deptFilter, roleFilter });
        }
    }
}