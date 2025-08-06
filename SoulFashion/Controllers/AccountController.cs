using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using Repositories.DTOs;
using System.Security.Claims;
using Services.Implementations;

[ApiController]
[Route("api/[controller]")]

public class AccountController : ControllerBase
{
    private readonly IAccountService _service;

    public AccountController(IAccountService service)
    {
        _service = service;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(UserLoginDto dto)
    {
        var token = await _service.LoginAsync(dto);
        return Ok(new { token });
    }

    [HttpPost("login-google")]
    public async Task<IActionResult> GoogleLogin(GoogleLoginDto dto)
    {
        var token = await _service.GoogleLoginAsync(dto);
        return Ok(new { token });
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(UserRegisterDto dto)
    {
        await _service.RegisterAsync(dto);
        return Ok("Đăng ký thành công");
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, UpdateAccountDto dto)
    {
        await _service.UpdateAsync(id, dto);
        return Ok("Đã cập nhật thành công");
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
    {
        await _service.ChangePasswordAsync(dto);
        return Ok("Đổi mật khẩu thành công");
    }

    [HttpGet]

    public async Task<IActionResult> GetAll()
    {
        var result = await _service.GetAllAsync();
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var result = await _service.GetByIdAsync(id);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }

    [HttpPut("update-avatar")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UpdateAvatar(int userId, [FromForm] UpdateAvatarDto dto)
    {
        var imageUrl = await _service.UpdateAvatarAsync(userId, dto.Avatar);
        return Ok(new { avatarUrl = imageUrl });
    }

    [HttpPut("{id}/change-role")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> ChangeRole(int id, [FromBody] ChangeRoleDto dto)
    {
        await _service.ChangeRoleAsync(id, dto.NewRole);
        return Ok(new { message = "Cập nhật vai trò thành công" });
    }

    [Authorize(Roles = "admin")]
    [HttpGet("verifications/pending")]
    public async Task<IActionResult> GetPendingVerifications()
    {
        try
        {
            var result = await _service.GetPendingVerificationsAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, "Lỗi server: " + ex.Message);
        }
    }


    [Authorize(Roles = "admin")]
    [HttpPut("verifications/approve/{userId}")]
    public async Task<IActionResult> ApproveVerification(int userId)
    {
        await _service.VerifyUserAsync(userId);
        return Ok("Xác minh thành công");
    }

    [HttpPost("verification")]
    [Authorize] // hoặc [Authorize(Roles = "customer,Collaborator")]
    public async Task<IActionResult> SubmitVerification([FromForm] SubmitVerificationDto dto)
    {
        var userId = int.Parse(User.FindFirst("id")!.Value); // hoặc JwtHelper.GetUserId(User)
        await _service.SubmitVerificationAsync(userId, dto);
        return Ok("Đã gửi thông tin xác minh");
    }

}
