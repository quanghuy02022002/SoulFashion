using Microsoft.AspNetCore.Http;
using Repositories.DTOs;
using Repositories.Models;

namespace Services.Interfaces
{
    public interface IAccountService
    {
        Task<string> LoginAsync(UserLoginDto dto);
        Task<string> GoogleLoginAsync(GoogleLoginDto dto);
        Task RegisterAsync(UserRegisterDto dto);
        Task UpdateAsync(int userId, UpdateAccountDto dto);
        Task<string> UpdateAvatarAsync(int userId, IFormFile avatar); // 👈 Avatar riêng biệt
        Task ChangePasswordAsync(ChangePasswordDto dto);
        Task<List<User>> GetAllAsync();
        Task<User?> GetByIdAsync(int id);
        Task DeleteAsync(int id);
    }
}
