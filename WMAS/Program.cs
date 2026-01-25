using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WMAS.Data;
using WMAS.Data.Seed;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentity<IdentityUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders().AddDefaultUI();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Events.OnValidatePrincipal = async context =>
    {
        var principal = context.Principal;
        if (principal == null) { return; }
        if (!principal.IsInRole("Employee")) { return; }

        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) { return; }
      
        var db = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
        var employee = await db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.UserId == userId);

        if (employee == null || !employee.IsActive || !employee.HasSystemAccess)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync();
        }
    };
});

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication(); 
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();
// Seed roles + admin
using (var scope = app.Services.CreateScope())
{
    await IdentitySeeder.SeedRolesAndAdminAsync(scope.ServiceProvider);
}

app.Run();
