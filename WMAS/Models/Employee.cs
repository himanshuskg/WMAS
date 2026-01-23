using Microsoft.AspNetCore.Identity;
using WMAS.Models.Common;

namespace WMAS.Models
{
    public class Employee : AuditableEntity
    {
        public int EmployeeId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public int DepartmentId { get; set; }
        public Department Department { get; set; }
        public int DesignationId { get; set; }
        public Designation Designation { get; set; }
        public string UserId { get; set; }
        public IdentityUser User { get; set; }
    }
}
