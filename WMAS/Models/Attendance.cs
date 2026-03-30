using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using WMAS.Models.Common;

namespace WMAS.Models
{
    public class Attendance : AuditableEntity
    {
        public int AttendanceId { get; set; }

        [Required]
        public int EmployeeId { get; set; }

        [ValidateNever]
        public Employee? Employee { get; set; }

        [Required]
        public DateTime Date { get; set; }

        public DateTime? CheckInTime { get; set; }
        public DateTime? CheckOutTime { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Present";
        public bool IsCheckedOut => CheckOutTime != null;
    }
}
