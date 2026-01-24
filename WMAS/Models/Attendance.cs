using System.ComponentModel.DataAnnotations;
using WMAS.Models.Common;

namespace WMAS.Models
{
    public class Attendance: AuditableEntity
    {
        public int AttendanceId { get; set; }

        [Required]
        public int EmployeeId { get; set; }
        public Employee Employee { get; set; }

        [Required]
        public DateTime Date { get; set; }

        [Required]
        public string Status { get; set; } = "Present";
    }
}
