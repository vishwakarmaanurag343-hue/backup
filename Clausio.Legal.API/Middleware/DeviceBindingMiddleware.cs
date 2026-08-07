using Clausio.Legal.Service;
using Clausio.Legal.Core.Entities;
using Clausio.Legal.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Clausio.Legal.API.Middleware;

public class DeviceBindingMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IAuthService authService, ClausioDbContext db)
    {
        try
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var claimFp = context.User.FindFirst("device_fp")?.Value;
                var currentFp = AuthService.ComputeDeviceFingerprint(
                    context.Request.Headers.UserAgent.ToString(),
                    context.Connection.RemoteIpAddress?.ToString());

                if (!string.IsNullOrEmpty(claimFp) && !string.Equals(claimFp, currentFp, StringComparison.Ordinal))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsJsonAsync(new
                    {
                        error = "Security Violation: Token is bound to another browser/device and cannot be used from this environment."
                    });
                    return;
                }

                // Active Sliding Session: Return fresh token in header for active user
                var userIdStr = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                    ?? context.User.FindFirst("sub")?.Value;
                if (Guid.TryParse(userIdStr, out var userId))
                {
                    var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
                    if (user != null)
                    {
                        var freshToken = authService.GenerateToken(
                            user, 
                            context.Request.Headers.UserAgent.ToString(), 
                            context.Connection.RemoteIpAddress?.ToString());
                        
                        context.Response.Headers["X-New-Token"] = freshToken;
                        context.Response.Headers["Access-Control-Expose-Headers"] = "X-New-Token";
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // Log & gracefully continue or handle auth middleware error
            Console.WriteLine($"[DeviceBindingMiddleware Error]: {ex.Message}");
        }

        await next(context);
    }
}
