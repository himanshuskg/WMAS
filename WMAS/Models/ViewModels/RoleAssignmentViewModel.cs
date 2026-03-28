namespace WMAS.Models.ViewModels
{
    public class RoleAssignmentViewModel
    {
        public int EmployeeId { get; set; }
        public string FullName { get; set; } = "";
        public string? Email { get; set; }
        public string? Department { get; set; }
        public bool IsActive { get; set; }

        public List<string> CurrentRoles { get; set; } = new();

        public bool IsHR => CurrentRoles.Contains("HR");
        public bool IsManager => CurrentRoles.Contains("Manager");
    }
}
