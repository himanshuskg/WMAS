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

        [ValidateNever]
        public Department Department { get; set; }

        [ValidateNever]
        public Designation Designation { get; set; }

        public string? UserId { get; set; }
    }
}
