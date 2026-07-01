using System.Net;

namespace WarehouseManager.Middleware;

public partial class LoggingMiddleware(RequestDelegate next, ILogger<LoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        LogMethodPathByRemoteIp(context.Request.Method, context.Request.Path, context.Connection.RemoteIpAddress);

        await next.Invoke(context);
    }

    [LoggerMessage(LogLevel.Information, "{Method} {Path} by {RemoteIp}")]
    partial void LogMethodPathByRemoteIp(string method, PathString path, IPAddress remoteIp);
}