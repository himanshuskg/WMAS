using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;
using System.Security.Claims;
using WMAS.Models;
using WMAS.Models.Common;

namespace WMAS.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IHttpContextAccessor httpContextAccessor) : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public DbSet<Department> Departments { get; set; }
        public DbSet<Designation> Designations { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<Leave> Leaves { get; set; }
        public DbSet<Salary> Salaries { get; set; }
        public DbSet<Note> Notes { get; set; }
        public DbSet<EmployeeType> EmployeeTypes { get; set; }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<Salary>(entity =>
            {
                entity.Property(e => e.BasicPay).HasPrecision(18, 2);
                entity.Property(e => e.Allowances).HasPrecision(18, 2);
                entity.Property(e => e.Deductions).HasPrecision(18, 2);
                entity.Property(e => e.NetSalary).HasPrecision(18, 2);
            });
            //Disable Cascade delete
            builder.Entity<Employee>().HasOne(e => e.Department)
                                      .WithMany().HasForeignKey(e => e.DepartmentId)
                                      .OnDelete(DeleteBehavior.Restrict);
            builder.Entity<Employee>().HasOne(e => e.ReportingManager)
                                      .WithMany(e => e.Subordinates)
                                      .HasForeignKey(e => e.ReportingManagerId)
                                      .OnDelete(DeleteBehavior.Restrict);
            builder.Entity<Employee>().HasOne(e => e.Designation)
                                      .WithMany().HasForeignKey(e => e.DesignationId)
                                      .OnDelete(DeleteBehavior.Restrict);
            builder.Entity<EmployeeType>().HasData(
                new EmployeeType { Id = 1, Name = "Permanent", IsActive = true },
                new EmployeeType { Id = 2, Name = "Temporary", IsActive = true },
                new EmployeeType { Id = 3, Name = "Contract" , IsActive = true },
                new EmployeeType { Id = 4, Name = "Intern" , IsActive = true }
            );
            // Seed Departments
            builder.Entity<Department>().HasData(
                new Department { DepartmentId = 1, DepartmentName = "IT" },
                new Department { DepartmentId = 2, DepartmentName = "HR" },
                new Department { DepartmentId = 3, DepartmentName = "Sales" },
                new Department { DepartmentId = 4, DepartmentName = "Admin" }
            );
            // Seed Designations with DepartmentId
            builder.Entity<Designation>().HasData(
                new Designation { DesignationId = 1, Title = "Software Developer", DepartmentId = 1 },
                new Designation { DesignationId = 2, Title = "System Administrator", DepartmentId = 1 },
                new Designation { DesignationId = 3, Title = "Network Engineer", DepartmentId = 1 },
                new Designation { DesignationId = 4, Title = "HR Specialist", DepartmentId = 2 },
                new Designation { DesignationId = 5, Title = "HR Manager", DepartmentId = 2 },
                new Designation { DesignationId = 6, Title = "Talent Acquisition Coordinator", DepartmentId = 2 },
                new Designation { DesignationId = 7, Title = "Sales Executive", DepartmentId = 3 },
                new Designation { DesignationId = 8, Title = "Sales Manager", DepartmentId = 3 },
                new Designation { DesignationId = 9, Title = "Account Executive", DepartmentId = 3 },
                new Designation { DesignationId = 10, Title = "Office Manager", DepartmentId = 4 },
                new Designation { DesignationId = 11, Title = "Executive Assistant", DepartmentId = 4 },
                new Designation { DesignationId = 12, Title = "Receptionist", DepartmentId = 4 }
            );

        }


        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            var entries = ChangeTracker.Entries<AuditableEntity>();
            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedOn = DateTime.UtcNow;
                    entry.Entity.CreatedBy = userId;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdatedOn = DateTime.UtcNow;
                    entry.Entity.UpdatedBy = userId;
                }
            }
            return await base.SaveChangesAsync(cancellationToken);
        }

    }
}
