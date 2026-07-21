using Formations.Application.Common.Interfaces;
using Formations.Domain.Entities;
using System.Security.Claims;

namespace Formations.Api.Middleware;

public class ActivityLogMiddleware
{
    private readonly RequestDelegate _next;

    public ActivityLogMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, IApplicationDbContext dbContext)
    {
        await _next(context);

        if (context.Request.Method != "GET" && context.Response.StatusCode < 400
            && context.User.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is not null)
            {
                dbContext.ActivityLogs.Add(new ActivityLog
                {
                    UserId = userId,
                    Action = $"{context.Request.Method} {context.Request.Path}",
                    IpAddress = context.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = context.Request.Headers.UserAgent.ToString().Truncate(500)
                });
                await dbContext.SaveChangesAsync();
            }
        }
    }
}

internal static class StringExtensions
{
    public static string Truncate(this string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength];
}
