using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using WMAS.Models.Common;

namespace WMAS.Models
{
    public class Salary :AuditableEntity
    {
        public int SalaryId { get; set; }

        [Required]
        public int EmployeeId { get; set; }

        [ValidateNever]
        public Employee? Employee { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal BasicPay { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Allowances { get; set; } = 0;

        [Range(0, double.MaxValue)]
        public decimal Deductions { get; set; } = 0;  // LOP amount in ₹

        public decimal NetSalary { get; set; }
        public int TotalDays { get; set; }  // calendar days: 31/30/28
        public int Sundays { get; set; }  // paid offs in month
        public int WorkingDays { get; set; }  // Mon-Sat days
        public int PresentDays { get; set; }  // full days (>=7h45m)
        public int HalfDays { get; set; }  // days <7h45m
        public int ApprovedLeave { get; set; }  // paid leave days
        public decimal LopDaysExact { get; set; }  // 1.5 = 1 absent + 1 half

        [Required]
        [Range(1, 12)]
        public int Month { get; set; }

        [Required]
        [Range(2000, 2100)]
        public int Year { get; set; }
        public bool IsFinalized { get; set; } = false;
    }
}