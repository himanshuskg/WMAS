using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WMAS.Models.Common;

namespace WMAS.Models
{
    public class Employee : AuditableEntity
    {
        public int EmployeeId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public DateOnly DateOfBirth { get; set; }
        public DateOnly JoiningDate { get; set; }
        public int DepartmentId { get; set; }
        public int DesignationId { get; set; }
        public string? UserId { get; set; }
        public bool HasSystemAccess { get; set; } = false;
        public bool IsActive { get; set; } = true;
        public DateTime? DeactivatedOn { get; set; }
        public string? GeneratedPassword { get; set; }
        public bool IsPasswordChanged { get; set; }
        public int PasswordResetCount { get; set; }
        public decimal Salary { get; set; }

        public int EmployeeTypeId { get; set; }
        public int? ReportingManagerId { get; set; }  // FK

        public Employee? ReportingManager { get; set; }  // Manager
        [ValidateNever]
        public ICollection<Employee> Subordinates { get; set; }

       
        public EmployeeType EmployeeType { get; set; } = null!;

        [ValidateNever]
        public Department Department { get; set; }

        [ValidateNever]
        public Designation Designation { get; set; }
    }
}
