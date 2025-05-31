using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Server.Circuits;

namespace BlazorAuthExample;

/// <summary>
///     Middleware that sets the BlazorSafeScopedServicesAccessor to the request scope when in traditional ASP.NET http
///     pipeline.
/// </summary>
/// <param name="servicesAccessor"></param>
public class BlazorSafeScopedServicesMiddleware(BlazorSafeScopedServicesAccessor servicesAccessor) : IMiddleware
{
    public Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        // for ASP.NET pipeline we set the services to the request scope
        servicesAccessor.Services = context.RequestServices;
        return next(context);
    }
}

/// <summary>
///     Circuit handler that sets the BlazorSafeScopedServicesAccessor to the circuit scope when in Blazor
///     InteractiveServer pipeline.
/// </summary>
/// <param name="services"></param>
/// <param name="servicesAccessor"></param>
public class BlazorSafeScopedServicesAccessorCircuitHandler(
    IServiceProvider services,
    BlazorSafeScopedServicesAccessor servicesAccessor)
    : CircuitHandler
{
    public override Func<CircuitInboundActivityContext, Task> CreateInboundActivityHandler(
        Func<CircuitInboundActivityContext, Task> next)
    {
        return async context =>
        {
            // For SignalR pipeline we set the services to the circuit scope
            servicesAccessor.Services = services;
            await next(context);
            servicesAccessor.Services = null;
        };
    }
}

/// <summary>
///     Service accessor that should be used when mixed components need access to the same services.
///     <remarks>
///         I.e. in a http pipeline the request scope is given, but in a blazor circuit the scope belongs to the
///         circuit which is stateful. This service is set in all three modes, so it is safe to use everywhere
///     </remarks>
/// </summary>
public class BlazorSafeScopedServicesAccessor
{
    [SuppressMessage("Performance", "CA1822:Mark members as static",
        Justification = "this needs to be an instance for better DI experience")]
    public IServiceProvider? Services
    {
        get => s_blazorServices.Value;
        set => s_blazorServices.Value = value;
    }

    private static readonly AsyncLocal<IServiceProvider?> s_blazorServices = new();
}
