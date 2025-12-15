namespace WesterUnionPD.Middleware;

/// <summary>
/// Blocks all requests except from allowed IPs.
/// </summary>
public sealed class IpWhitelistMiddleware
{
    private readonly RequestDelegate _next;
    private readonly HashSet<string> _allowedIps;

    public IpWhitelistMiddleware(RequestDelegate next, IConfiguration config)
    {
        _next = next;
        _allowedIps = config
            .GetSection("Upload:AllowedIPs")
            .Get<string[]>()!
            .ToHashSet();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var ip = context.Connection.RemoteIpAddress?.MapToIPv4().ToString();

        if (ip == null || !_allowedIps.Contains(ip))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("Access denied: IP not allowed.");
            return;
        }

        await _next(context);
    }
}
