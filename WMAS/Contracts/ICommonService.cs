using WMAS.Models;
using WMAS.Models.Common;

namespace WMAS.Contracts
{
    public interface ICommonService
    {
        Task<UserProvisionResult> CreateUserAsync(string email);
        Task<UserProvisionResult> ResetPasswordAsync(string userId);
    }
}
