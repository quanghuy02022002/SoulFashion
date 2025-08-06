using Repositories.DTOs;
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
        private static UserDto MapToDto(User user)
        {
            return new UserDto
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                AvatarUrl = user.AvatarUrl,
                Role = user.Role,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                CCCD = user.UserVerification?.CCCD,
                Address = user.UserVerification?.Address,
                Verified = user.UserVerification?.Verified
            };
        }

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

            // Cập nhật thông tin cơ bản
            user.FullName = dto.FullName ?? user.FullName;
            user.Phone = dto.Phone ?? user.Phone;
            user.UpdatedAt = DateTime.UtcNow;

            // Xử lý phần xác minh nếu có thông tin CCCD / địa chỉ / ảnh
            if (!string.IsNullOrWhiteSpace(dto.CCCD) || !string.IsNullOrWhiteSpace(dto.Address) || dto.VerificationImage != null)
            {
                var verification = user.UserVerification ?? new UserVerification
                {
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                };

                verification.CCCD = dto.CCCD ?? verification.CCCD;
                verification.Address = dto.Address ?? verification.Address;

                if (dto.VerificationImage != null)
                {
                    var filename = $"verification_user_{userId}_{DateTime.UtcNow.Ticks}";
                    var imageUrl = await _s3Service.UploadFileAsync(dto.VerificationImage, filename);
                    verification.ImageUrl = imageUrl;
                }

                verification.Verified = false; // không cho user tự set verified
                verification.CreatedAt = DateTime.UtcNow;

                user.UserVerification = verification;
            }

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

        public async Task<List<UserDto>> GetAllAsync()
        {
            var users = await _repo.GetAllAsync();
            return users.Select(MapToDto).ToList();
        }

        public async Task<UserDto?> GetByIdAsync(int id)
        {
            var user = await _repo.GetByIdAsync(id);
            return user == null ? null : MapToDto(user);
        }

        public async Task DeleteAsync(int id)
        {
            var user = await _repo.GetByIdAsync(id);
            if (user == null) throw new Exception("Không tìm thấy");

            await _repo.DeleteAsync(user);
        }
        public async Task ChangeRoleAsync(int userId, string newRole)
        {
            var user = await _repo.GetByIdAsync(userId) ?? throw new Exception("Không tìm thấy người dùng");

            // Danh sách role hợp lệ (phân biệt đúng định dạng)
            var allowedRoles = new[] { "customer", "admin", "Collaborator" };

            // Tìm vai trò khớp không phân biệt hoa thường
            var matchedRole = allowedRoles.FirstOrDefault(r => r.Equals(newRole, StringComparison.OrdinalIgnoreCase));
            if (matchedRole == null)
                throw new Exception("Vai trò không hợp lệ");

            user.Role = matchedRole; // Gán đúng format chuẩn
            user.UpdatedAt = DateTime.UtcNow;

            await _repo.UpdateAsync(user);
        }
        public async Task<List<UserVerification>> GetPendingVerificationsAsync()
        {
            try
            {
                var all = await _repo.GetAllAsync();
                return all
                    .Where(u => u.UserVerification != null && u.UserVerification.Verified == false)
                    .Select(u => u.UserVerification!) // thêm dấu ! để nói với compiler là không null
                    .ToList();
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi khi lấy danh sách xác minh: " + ex.Message);
            }
        }


        public async Task VerifyUserAsync(int userId)
        {
            var user = await _repo.GetByIdAsync(userId) ?? throw new Exception("Không tìm thấy người dùng");
            if (user.UserVerification == null)
                throw new Exception("Người dùng chưa gửi thông tin xác minh");

            user.UserVerification.Verified = true;
            user.UserVerification.CreatedAt = DateTime.UtcNow;

            await _repo.UpdateAsync(user);
        }

    }
}
