using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
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
        public DateTime? ActionOn { get; set; }  
        public int? ActionById { get; set; }  

        [ForeignKey("ActionById")]
        [ValidateNever]
        public Employee? ActionBy { get; set; }

        [StringLength(250)]
        public string? ActionComments{ get; set; }
    }
}
