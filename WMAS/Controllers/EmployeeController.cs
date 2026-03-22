using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WMAS.Contracts;
using WMAS.Data;
using WMAS.Models;
using WMAS.Models.ViewModels;

namespace WMAS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class EmployeeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICommonService _commonService;
        private readonly IEncryptionService _encryptionService;

        public EmployeeController(ApplicationDbContext context,
                                  ICommonService commonService,
                                  IEncryptionService encryptionService)
        {
            _context = context;
            _commonService = commonService;
            _encryptionService = encryptionService;
        }

        // ================= LIST =================
        public async Task<IActionResult> Index(
    string? searchTerm,
    List<int>? SelectedDepartmentIds,
    List<int>? SelectedDesignationIds,
    List<int>? SelectedEmployeeTypeIds,
    int pageNumber = 1,
    int pageSize = 5)
        {
            var query = _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Designation)
                .Include(e => e.EmployeeType)
                .AsQueryable();

            // 🔍 Search
            if (!string.IsNullOrWhiteSpace(searchTerm))
                query = query.Where(e => e.FullName.Contains(searchTerm));

            // 🏢 Department filter
            if (SelectedDepartmentIds != null && SelectedDepartmentIds.Any(id => id > 0))
                query = query.Where(e => SelectedDepartmentIds.Contains(e.DepartmentId));

            // 💼 Designation filter
            if (SelectedDesignationIds != null && SelectedDesignationIds.Any(id => id > 0))
                query = query.Where(e => SelectedDesignationIds.Contains(e.DesignationId));

            // 🧑‍💼 Employee Type filter
            if (SelectedEmployeeTypeIds != null && SelectedEmployeeTypeIds.Any(id => id > 0))
                query = query.Where(e => SelectedEmployeeTypeIds.Contains(e.EmployeeTypeId));

            var totalCount = await query.CountAsync();

            var employees = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();

            var departments = await _context.Departments.AsNoTracking().ToListAsync();
            var designations = await _context.Designations.AsNoTracking().ToListAsync();
            var employeeTypes = await _context.EmployeeTypes.AsNoTracking().ToListAsync();

            var vm = new EmployeeListViewModel
            {
                Employees = employees,
                PageNumber = pageNumber,
                PageSize = pageSize,
                Total = totalCount,
                TotalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize)),
                SearchTerm = searchTerm,
                SelectedDepartmentIds = SelectedDepartmentIds ?? new List<int>(),
                SelectedDesignationIds = SelectedDesignationIds ?? new List<int>(),
                SelectedEmployeeTypeIds = SelectedEmployeeTypeIds ?? new List<int>(),
                Departments = departments,
                Designations = designations,
                EmployeeTypes = employeeTypes
            };

            return View(vm);
        }

        // ================= DETAILS =================
        public async Task<IActionResult> Details(int id)
        {
            var employee = await _context.Employees
                .Include(e => e.Department)
                .Include(e => e.Designation)
                .Include(e => e.EmployeeType)
                .FirstOrDefaultAsync(e => e.EmployeeId == id);

            if (employee == null)
                return NotFound();

            // Decrypt password for admin view
            if (!string.IsNullOrEmpty(employee.GeneratedPassword))
            {
                employee.GeneratedPassword =
                    _encryptionService.Decrypt(employee.GeneratedPassword);
            }

            return View(employee);
        }

        // ================= CREATE (GET) =================
        public async Task<IActionResult> Create()
        {
            var vm = new EmployeeCreateUpdateViewModel();
            await LoadDropdownsAsync(vm);
            return View(vm);
        }

        // ================= CREATE (POST) =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EmployeeCreateUpdateViewModel request)
        {
            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync(request);
                return View(request);
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                string? encryptedPassword = null;
                string? userId = null;

                // ✅ Create user FIRST
                if (request.HasSystemAccess)
                {
                    var result = await _commonService.CreateUserAsync(request.Email);

                    if (!result.Success)
                    {
                        ModelState.AddModelError("", "Failed to create system user.");
                        await LoadDropdownsAsync(request);
                        return View(request);
                    }

                    userId = result.UserId;

                    if (!string.IsNullOrEmpty(result.Password))
                    {
                        encryptedPassword =
                            _encryptionService.Encrypt(result.Password);
                    }

                    TempData["Success"] =
                        $"Employee created. Temporary password: {result.Password}";
                }

                // ✅ Save employee ONCE
                var employee = new Employee
                {
                    FullName = request.FullName,
                    Email = request.Email,
                    Phone = request.Phone,
                    Gender = request.Gender,
                    DateOfBirth = request.DateOfBirth,
                    JoiningDate = request.HireDate,
                    DepartmentId = request.DepartmentId,
                    DesignationId = request.DesignationId,
                    EmployeeTypeId = request.EmployeeTypeId,
                    Salary = request.Salary,
                    HasSystemAccess = request.HasSystemAccess,
                    IsActive = request.IsActive,

                    UserId = userId,
                    GeneratedPassword = encryptedPassword,
                    IsPasswordChanged = false,
                    PasswordResetCount = 0
                };

                _context.Employees.Add(employee);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // ================= EDIT (GET) =================
        public async Task<IActionResult> Edit(int id)
        {
            var employee = await _context.Employees.FindAsync(id);

            if (employee == null)
                return NotFound();

            var vm = new EmployeeCreateUpdateViewModel
            {
                Id = employee.EmployeeId, // ✅ FIXED
                FullName = employee.FullName,
                Email = employee.Email,
                Phone = employee.Phone,
                Gender = employee.Gender,
                DateOfBirth = employee.DateOfBirth,
                HireDate = employee.JoiningDate,
                DepartmentId = employee.DepartmentId,
                DesignationId = employee.DesignationId,
                EmployeeTypeId = employee.EmployeeTypeId,
                Salary = employee.Salary,
                HasSystemAccess = employee.HasSystemAccess,
                IsActive = employee.IsActive
            };

            await LoadDropdownsAsync(vm);

            return View(vm);
        }

        // ================= EDIT (POST) =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EmployeeCreateUpdateViewModel model)
        {
            if (model.Id == null)
                return BadRequest();

            var employee = await _context.Employees.FindAsync(model.Id);

            if (employee == null)
                return NotFound();

            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync(model);
                return View(model);
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                employee.FullName = model.FullName;
                employee.Email = model.Email;
                employee.Phone = model.Phone;
                employee.Gender = model.Gender;
                employee.DateOfBirth = model.DateOfBirth;
                employee.JoiningDate = model.HireDate;
                employee.DepartmentId = model.DepartmentId;
                employee.DesignationId = model.DesignationId;
                employee.EmployeeTypeId = model.EmployeeTypeId;
                employee.Salary = model.Salary;
                employee.IsActive = model.IsActive;

                // ✅ System access logic
                if (!employee.HasSystemAccess && model.HasSystemAccess)
                {
                    var result = await _commonService.CreateUserAsync(employee.Email);

                    if (!result.Success)
                    {
                        ModelState.AddModelError("", "Failed to grant system access.");
                        await LoadDropdownsAsync(model);
                        return View(model);
                    }

                    employee.HasSystemAccess = true;
                    employee.UserId = result.UserId;

                    if (!string.IsNullOrEmpty(result.Password))
                    {
                        employee.GeneratedPassword =
                            _encryptionService.Encrypt(result.Password);
                    }

                    employee.IsPasswordChanged = false;
                    employee.PasswordResetCount = 0;

                    TempData["Success"] =
                        $"System access granted. Temporary password: {result.Password}";
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        // ================= RESET PASSWORD =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(int id)
        {
            var employee = await _context.Employees.FindAsync(id);

            if (employee == null || string.IsNullOrEmpty(employee.UserId))
                return RedirectToAction(nameof(Details), new { id });

            var result = await _commonService.ResetPasswordAsync(employee.UserId);

            if (!result.Success)
            {
                TempData["Error"] = "Password reset failed.";
                return RedirectToAction(nameof(Details), new { id });
            }

            employee.GeneratedPassword =
                _encryptionService.Encrypt(result.Password);

            employee.IsPasswordChanged = false;
            employee.PasswordResetCount++;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Password reset successfully. Temporary password: {result.Password}";

            return RedirectToAction(nameof(Details), new { id });
        }

        // ================= ACTIVATE =================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null) return NotFound();

            employee.IsActive = true;
            employee.DeactivatedOn = null;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Employee activated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // ================= DEACTIVATE =================
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

        // ================= DROPDOWNS =================
        private async Task LoadDropdownsAsync(EmployeeCreateUpdateViewModel vm)
        {
            vm.Departments = await _context.Departments.ToListAsync();
            vm.Designations = await _context.Designations.ToListAsync();
            vm.EmployeeTypes = await _context.EmployeeTypes.ToListAsync();
        }
    }
}