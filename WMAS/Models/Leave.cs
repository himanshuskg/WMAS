using System.ComponentModel.DataAnnotations;
using WMAS.Models.Common;

namespace WMAS.Models
{
    public class Leave : AuditableEntity
    {
        public int LeaveId { get; set; }

        [Required]
        public int EmployeeId { get; set; }
        public Employee? Employee { get; set; }

        [Required]
        public DateTime FromDate { get; set; }

        [Required]
        public DateTime ToDate { get; set; }

        [Required]
        [StringLength(20)]
        public string LeaveType { get; set; } = "Casual";

        [StringLength(250)]
        public string? Reason { get; set; }
        public string Status { get; set; } = "Pending";
    }
}
