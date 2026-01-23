using System.ComponentModel.DataAnnotations;
using WMAS.Models.Common;

namespace WMAS.Models
{
    public class Department : AuditableEntity
    {
        public int DepartmentId { get; set; }
        [Required]
        [StringLength(100)]
        public string DepartmentName { get; set; }
    }
}
