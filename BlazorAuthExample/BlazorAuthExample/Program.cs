using BlazorAuthExample;
using BlazorAuthExample.Auth;
using BlazorAuthExample.Components;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.DependencyInjection.Extensions;
using _Imports = BlazorAuthExample.Client._Imports;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
builder.TestAuthorizeAttributeOnPages();
builder.Services.AddScoped<UserEnrichMiddleware>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHttpContextAccessor();
// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services
    .AddAuthentication(CustomAuthenticationOptions.SchemeName)
    .AddScheme<CustomAuthenticationOptions, CustomAuthenticationHandler>(
        CustomAuthenticationOptions.SchemeName, _ => { });
var defaultPolicy = new AuthorizationPolicyBuilder()
    .RequireAuthenticatedUser()
    .Build();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
builder.Services.AddSingleton<IAuthorizationHandler, FallbackAuthorizationHandler>();
builder.Services.AddAuthorizationBuilder()
    .AddDefaultPolicy(Policies.DefaultPolicy, defaultPolicy)
    .AddFallbackPolicy(Policies.FallbackPolicy,
        policy => policy.AddRequirements(new EvaluateDefaultPolicyRequirement()))
    ;

builder.Services.AddOptions<JwtOptions>()
    .Bind(configuration.GetSection(JwtOptions.SectionName));
builder.Services.AddSingleton<JwtGenerator>();

builder.Services.AddScoped<BlazorSafeScopedServicesAccessor>();
builder.Services.TryAddEnumerable(
    ServiceDescriptor.Scoped<CircuitHandler, BlazorSafeScopedServicesAccessorCircuitHandler>());
builder.Services.AddScoped<BlazorSafeScopedServicesMiddleware>();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<JwtTokenProvider>();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.TryAddEnumerable(
    ServiceDescriptor.Scoped<CircuitHandler, UserCircuitHandler>());
builder.Services.AddHealthChecks();
var app = builder.Build();

app.UseMiddleware<BlazorSafeScopedServicesMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseAntiforgery();

app.MapStaticAssets()
    // This is needed, because the fallback policy applies to all endpoints
    .AllowAnonymous()
    ;

app.UseAuthentication();
app.UseMiddleware<UserEnrichMiddleware>();
app.UseAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(_Imports).Assembly)
    /* this is counterintuitive, but required
         see https://github.com/dotnet/aspnetcore/issues/52063
         and https://github.com/dotnet/AspNetCore.Docs/issues/31931
        */
    .AllowAnonymous()
    ;

app.MapHealthChecks("/api/healthz").AllowAnonymous();

app.Run();
