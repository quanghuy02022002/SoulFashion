﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using Repositories.DTOs;
using System.Security.Claims;

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

}
