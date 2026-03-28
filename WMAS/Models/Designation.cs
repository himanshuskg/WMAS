using System.ComponentModel.DataAnnotations;
using WMAS.Models.Common;

namespace WMAS.Models
{
    public class Designation : AuditableEntity
    {
        public int DesignationId { get; set; }
        public int DepartmentId { get; set; }

        [Required(ErrorMessage = "Designation Name is required")]
        [StringLength(100, ErrorMessage = "Designation Name cannot be longer than 100 characters")]
        public string Title { get; set; }
        public bool IsActive { get; set; } = true;

    }
}
