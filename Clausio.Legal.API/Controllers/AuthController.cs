using Clausio.Legal.API.Extensions;
using Clausio.Legal.Core.Dtos;
using Clausio.Legal.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clausio.Legal.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var userAgent = Request.Headers.UserAgent.ToString();
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var result = await authService.RegisterAsync(dto, userAgent, ipAddress, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message ?? "An error occurred during registration." });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var userAgent = Request.Headers.UserAgent.ToString();
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var result = await authService.LoginAsync(dto, userAgent, ipAddress, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message ?? "An error occurred during login." });
        }
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        try
        {
            var userId = User.GetUserId();
            var user = await authService.GetCurrentUserAsync(userId, cancellationToken);
            return user is null ? NotFound(new { message = "User not found." }) : Ok(user);
        }
        catch (Exception ex)
        {
            return Unauthorized(new { message = ex.Message ?? "Unauthorized user token." });
        }
    }

    [Authorize]
    [HttpPut("change-password")]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordDto dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = User.GetUserId();
            await authService.ChangePasswordAsync(userId, dto, cancellationToken);
            return Ok(new { message = "Password changed successfully." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message ?? "Failed to change password." });
        }
    }
}
