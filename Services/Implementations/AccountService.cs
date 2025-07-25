﻿using Repositories.DTOs;
using Repositories.Interfaces;
using Repositories.Models;
using Services.Interfaces;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Http;

namespace Services.Implementations
{
    public class AccountService : IAccountService
    {
        private readonly IUserRepository _repo;
        private readonly TokenService _tokenService;
        private readonly IS3Service _s3Service;

        public AccountService(IUserRepository repo, TokenService tokenService, IS3Service s3Service)
        {
            _repo = repo;
            _tokenService = tokenService;
            _s3Service = s3Service;
        }

        public async Task<string> LoginAsync(UserLoginDto dto)
        {
            var user = await _repo.GetByEmailAsync(dto.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                throw new Exception("Sai email hoặc mật khẩu");

            return _tokenService.CreateToken(user);
        }

        public async Task<string> GoogleLoginAsync(GoogleLoginDto dto)
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(dto.IdToken);

            var user = await _repo.GetByEmailAsync(payload.Email);
            if (user == null)
            {
                user = new User
                {
                    Email = payload.Email,
                    FullName = payload.Name,
                    AvatarUrl = payload.Picture,
                    Role = "customer",
                    PasswordHash = "", // không cần mật khẩu
                    CreatedAt = DateTime.UtcNow
                };
                await _repo.AddAsync(user);
            }

            return _tokenService.CreateToken(user);
        }

        public async Task RegisterAsync(UserRegisterDto dto)
        {
            if (await _repo.GetByEmailAsync(dto.Email) != null)
                throw new Exception("Email đã tồn tại");

            var user = new User
            {
                FullName = dto.FullName,
                Email = dto.Email,
                Phone = dto.Phone,
                Role = "customer",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                CreatedAt = DateTime.UtcNow
            };
            await _repo.AddAsync(user);
        }

        public async Task UpdateAsync(int userId, UpdateAccountDto dto)
        {
            var user = await _repo.GetByIdAsync(userId) ?? throw new Exception("Không tìm thấy người dùng");

            user.FullName = dto.FullName ?? user.FullName;
            user.Phone = dto.Phone ?? user.Phone;
            user.UpdatedAt = DateTime.UtcNow;

            await _repo.UpdateAsync(user);
        }

        public async Task<string> UpdateAvatarAsync(int userId, IFormFile avatar)
        {
            var user = await _repo.GetByIdAsync(userId) ?? throw new Exception("Không tìm thấy người dùng");

            var fileName = $"avatar_user_{userId}";
            var imageUrl = await _s3Service.UploadFileAsync(avatar, fileName);

            user.AvatarUrl = imageUrl;
            user.UpdatedAt = DateTime.UtcNow;

            await _repo.UpdateAsync(user);
            return imageUrl;
        }


        public async Task ChangePasswordAsync(ChangePasswordDto dto)
        {
            var user = await _repo.GetByEmailAsync(dto.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.OldPassword, user.PasswordHash))
                throw new Exception("Mật khẩu cũ không đúng");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            await _repo.SaveAsync();
        }

        public Task<List<User>> GetAllAsync() => _repo.GetAllAsync();

        public Task<User?> GetByIdAsync(int id) => _repo.GetByIdAsync(id);

        public async Task DeleteAsync(int id)
        {
            var user = await _repo.GetByIdAsync(id);
            if (user == null) throw new Exception("Không tìm thấy");

            await _repo.DeleteAsync(user);
        }
    }
}
