using WMAS.Models.Common;

namespace WMAS.Models
{
    public class Designation : AuditableEntity
    {
        public int DesignationId { get; set; }
        public string Title { get; set; }
    }
}
