namespace FinTrackPortal.API.Helpers;

/// <summary>
/// Client IP behind reverse proxies (Azure App Service, nginx, load balancers).
/// Matches FinShare “What To Do &amp; Where” spec (<c>X-Forwarded-For</c> first).
/// </summary>
public static class IpHelper
{
    /// <summary>First public client IP from <c>X-Forwarded-For</c>, or <see cref="ConnectionInfo.RemoteIpAddress"/>.</summary>
    public static string GetClientIp(HttpContext context)
    {
        var forwarded = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwarded))
            return forwarded.Split(',')[0].Trim();
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
