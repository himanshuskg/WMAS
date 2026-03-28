using WMAS.Models;

namespace WMAS.Models.ViewModels
{
    public class EmployeeListViewModel
    {
        public List<Employee>? Employees { get; set; }
        public int TotalPages { get; set; }
        public string? SearchTerm { get; set; }
        public List<int>? SelectedDepartmentIds { get; set; }   
        public List<int>? SelectedDesignationIds { get; set; } 
        public List<int>? SelectedEmployeeTypeIds { get; set; } 
        public int PageSize { get; set; } = 10;
        public List<int> PageSizeOptions { get; set; } = new List<int> { 3, 5, 10, 15, 20 };
        public int PageNumber { get; set; }
        public int Total { get; set; }
        public List<Department>? Departments { get; set; }
        public List<Designation>? Designations { get; set; }
        public List<EmployeeType>? EmployeeTypes { get; set; }
    }
}
