using System.ComponentModel.DataAnnotations;
using WMAS.Models.Common;

namespace WMAS.Models
{
    public class Department : AuditableEntity
    {
        public int DepartmentId { get; set; }

        [Required(ErrorMessage = "Department Name is required")]
        [StringLength(100, ErrorMessage = "Department Name cannot be longer than 100 characters")]
        public string DepartmentName { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
