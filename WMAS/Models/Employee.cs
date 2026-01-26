using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using WMAS.Models.Common;

namespace WMAS.Models
{
    public class Employee : AuditableEntity
    {
        public int EmployeeId { get; set; }

        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Phone { get; set; } = string.Empty;

        [Required]
        public int DepartmentId { get; set; }

        [Required]
        public int DesignationId { get; set; }
        public string? UserId { get; set; }
        public bool HasSystemAccess { get; set; } = false;
        public bool IsActive { get; set; } = true;
        public DateTime? DeactivatedOn { get; set; }
        public string? GeneratedPassword { get; set; }   
        public bool IsPasswordChanged { get; set; }
        public int PasswordResetCount { get; set; }
        
        [ValidateNever]
        public Department Department { get; set; }

        [ValidateNever]
        public Designation Designation { get; set; }
    }
}
