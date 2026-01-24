using System.ComponentModel.DataAnnotations;
using WMAS.Models.Common;

namespace WMAS.Models
{
    public class Salary : AuditableEntity
    {
        public int SalaryId { get; set; }

        [Required]
        public int EmployeeId { get; set; }
        public Employee Employee { get; set; }

        [Required]
        public decimal BasicPay { get; set; }

        public decimal Allowances { get; set; }
        public decimal Deductions { get; set; }

        [Required]
        public decimal NetSalary { get; set; }

        [Required]
        public int Month { get; set; }

        [Required]
        public int Year { get; set; }
    }
}
