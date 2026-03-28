using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using WMAS.Models.Common;

namespace WMAS.Models
{
    public class EmployeeType : AuditableEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public bool IsActive { get; set; }

        [ValidateNever]
        public List<Employee>? Employees { get; set; }
    }
}
