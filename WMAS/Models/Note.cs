using System.ComponentModel.DataAnnotations;
using WMAS.Models.Common;

namespace WMAS.Models
{
    public class Note : AuditableEntity
    {
        public int NoteId { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        [StringLength(500)]
        public string Content { get; set; }
    }
}