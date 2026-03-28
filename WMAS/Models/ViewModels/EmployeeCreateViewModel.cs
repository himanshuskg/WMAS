using System.ComponentModel.DataAnnotations;
using WMAS.Models;

namespace WMAS.Models.ViewModels
{
    public class EmployeeCreateUpdateViewModel
    {
        public int? Id { get; set; }
        [Display(Name = "Full Name")]
        [Required(ErrorMessage = "Full Name is required")]
        [StringLength(100)]
        public string FullName { get; set; } = null!;
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress]
        public string Email { get; set; } = null!;
        [Required(ErrorMessage = "Phone No. is required")]
        public string Phone { get; set; } = string.Empty;
        [Display(Name = "Department")]
        [Required(ErrorMessage = "Department is required")]
        public int DepartmentId { get; set; }
        [Display(Name = "Designation")]
        [Required(ErrorMessage = "Designation is required")]
        public int DesignationId { get; set; }
        [Display(Name = "Hire Date")]
        [Required(ErrorMessage = "Hire Date is required")]
        [DataType(DataType.Date)]
        [CustomValidation(typeof(EmployeeCreateUpdateViewModel), nameof(ValidateHireDate))]
        public DateOnly HireDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
        [Display(Name = "Date of Birth")]
        [Required(ErrorMessage = "Date of Birth is required")]
        [DataType(DataType.Date)]
        [CustomValidation(typeof(EmployeeCreateUpdateViewModel), nameof(ValidateDateOfBirth))]
        public DateOnly DateOfBirth { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddYears(-60));
        [Display(Name = "Employee Type")]
        [Required(ErrorMessage = "Employee Type is required")]
        public int EmployeeTypeId { get; set; }
        public int? ReportingManagerId { get; set; }

        [Required(ErrorMessage = "Gender is required")]
        public string Gender { get; set; } = null!;
        [Required(ErrorMessage = "Salary is required")]
        [Range(0, double.MaxValue)]
        public decimal Salary { get; set; }
        public bool HasSystemAccess { get; set; } = false;
        public bool IsActive { get; set; } = true;
        // For dropdown lists
        public List<Department>? Departments { get; set; }
        public List<Designation>? Designations { get; set; }
        public List<EmployeeType>? EmployeeTypes { get; set; }
        public List<Employee> Managers { get; set; } = new();
        // Validation methods:
        public static ValidationResult? ValidateHireDate(DateOnly? hireDate, ValidationContext context)
        {
            if (!hireDate.HasValue)
            {
                return new ValidationResult("Hire Date is required.");
            }
            var today = DateOnly.FromDateTime(DateTime.Today);
            if (hireDate.Value > today)
            {
                return new ValidationResult("Hire Date cannot be in the future.");
            }

            return ValidationResult.Success;
        }

        public static ValidationResult? ValidateDateOfBirth(DateOnly? dob, ValidationContext context)
        {
            if (!dob.HasValue)
            {
                return new ValidationResult("Date of Birth is required.");
            }
            var today = DateOnly.FromDateTime(DateTime.Today);
            var minDob = today.AddYears(-60);   
            var maxDob = today.AddYears(-18);   

            if (dob.Value < minDob || dob.Value > maxDob)
            {
                return new ValidationResult(
                    $"Date of Birth must be between {minDob:yyyy-MM-dd} and {maxDob:yyyy-MM-dd} " +
                    $"to be between 18 and 60 years old."
                );
            }

            return ValidationResult.Success;
        }
    }
}