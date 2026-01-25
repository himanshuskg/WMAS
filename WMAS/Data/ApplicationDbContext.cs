using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WMAS.Models;
using WMAS.Models.Common;

namespace WMAS.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options,IHttpContextAccessor httpContextAccessor) : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public DbSet<Department> Departments { get; set; }
        public DbSet<Designation> Designations { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<Attendance> Attendances{ get; set; }
        public DbSet<Leave> Leaves { get; set; }
        public DbSet<Salary> Salaries{ get; set; }
        public DbSet<Note> Notes{ get; set; }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Salary>(entity =>
            {
                entity.Property(e => e.BasicPay)
                      .HasPrecision(18, 2);

                entity.Property(e => e.Allowances)
                      .HasPrecision(18, 2);

                entity.Property(e => e.Deductions)
                      .HasPrecision(18, 2);

                entity.Property(e => e.NetSalary)
                      .HasPrecision(18, 2);
            });
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
