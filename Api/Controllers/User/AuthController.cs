using Application.Features.Auth;
using Application.Interfaces;
using Infrastructure.Redis.Services;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IRateLimitService _rateLimitService;

    public AuthController(IMediator mediator, IRateLimitService rateLimitService)
    {
        _mediator = mediator;
        _rateLimitService = rateLimitService;
    }
    

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginCommand command)
    {
        // Giới hạn: 1 IP chỉ được thử đăng nhập 5 lần mỗi phút
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        bool isAllowed = await _rateLimitService.IsAllowedAsync($"login_{clientIp}", 5, TimeSpan.FromMinutes(1));

        if (!isAllowed) return StatusCode(429, "Bạn spam quá nhanh, vui lòng thử lại sau 1 phút!");
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
            return Unauthorized(new { result.Message, result.Errors });

        return Ok(result);
    }


    [HttpPost("refresh_token")]
    public async Task<IActionResult> Refresht([FromBody] RefreshTokenCommand command)
    {
        // Giới hạn: 1 IP chỉ được thử đăng nhập 5 lần mỗi phút
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        bool isAllowed = await _rateLimitService.IsAllowedAsync($"login_{clientIp}", 5, TimeSpan.FromMinutes(1));

        if (!isAllowed) return StatusCode(429, "Bạn spam quá nhanh, vui lòng thử lại sau 1 phút!");
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
            return Unauthorized(new { result.Message, result.Errors });

        return Ok(result);
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterCommand command)
    {
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
            return BadRequest(new { result.Message, result.Errors });

        return Ok(new { result.Message });
    }

    // -----------------------------------------------------
    // API 1: CHỈ ĐĂNG XUẤT THIẾT BỊ HIỆN TẠI
    // -----------------------------------------------------
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
    {
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        bool isAllowed = await _rateLimitService.IsAllowedAsync($"logout_{clientIp}", 5, TimeSpan.FromMinutes(1));
        if (!isAllowed) return StatusCode(429, "Bạn spam quá nhanh, vui lòng thử lại sau 1 phút!");

        // Khởi tạo Record mới, truyền cứng false vào tham số thứ 2
        var command = new LogoutCommand(request);
        var result = await _mediator.Send(command);

        return Ok(new { message = result });
    }

    // -----------------------------------------------------
    // API 2: ĐĂNG XUẤT TẤT CẢ THIẾT BỊ
    // -----------------------------------------------------
    [HttpPost("logout-all")] // ĐỔI TÊN ĐƯỜNG DẪN TRÁNH TRÙNG LẶP
    [Authorize]
    public async Task<IActionResult> LogoutAll([FromBody] LogoutRequest request)
    {
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        bool isAllowed = await _rateLimitService.IsAllowedAsync($"logout_{clientIp}", 5, TimeSpan.FromMinutes(1));
        if (!isAllowed) return StatusCode(429, "Bạn spam quá nhanh, vui lòng thử lại sau 1 phút!");

        // Khởi tạo Record mới, truyền cứng true vào tham số thứ 2
        var command = new LogoutCommand(request);
        var result = await _mediator.Send(command);

        return Ok(new { message = result });
    }
}