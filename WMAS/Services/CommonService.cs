using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WMAS.Contracts;
using WMAS.Data;
using WMAS.Models;
using WMAS.Models.Common;

namespace WMAS.Services
{
    public class CommonService : ICommonService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public CommonService(UserManager<IdentityUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }
       
        public async Task<UserProvisionResult> CreateUserAsync(string email)
        {
            var user = new IdentityUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var password = GeneratePassword();

            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                return new UserProvisionResult { Success = false };
            }

            await _userManager.AddToRoleAsync(user, "Employee");

            return new UserProvisionResult
            {
                Success = true,
                Password = password,
                UserId = user.Id
            };
        }
        public async Task<UserProvisionResult> ResetPasswordAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return new UserProvisionResult { Success = false };

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var password = GeneratePassword();

            var result = await _userManager.ResetPasswordAsync(user, token, password);
            if (!result.Succeeded) return new UserProvisionResult { Success = false };

            return new UserProvisionResult
            {
                Success = true,
                Password = password,
                UserId = user.Id
            };
        }

        private string GeneratePassword()
        {
            return $"Emp@{Guid.NewGuid().ToString("N")[..8]}";
        }

    }
}
