using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Hrm.Contracts;
using Hrm.Web;
using Hrm.Web.Authorization;
using Hrm.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? builder.HostEnvironment.BaseAddress;
builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });
builder.Services.AddScoped<ISystemApiClient, SystemApiClient>();
builder.Services.AddAuthorizationCore(options =>
{
    foreach (var permission in PermissionCodes.All)
    {
        options.AddPolicy(permission, policy => policy.RequireAssertion(context =>
            context.User.IsInRole("ADMIN") || context.User.HasClaim("permission", permission)));
    }
});
builder.Services.AddScoped<BrowserSessionStore>();
builder.Services.AddScoped<AppAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(provider =>
    provider.GetRequiredService<AppAuthenticationStateProvider>());
builder.Services.AddScoped<IAuthApiClient, AuthApiClient>();
builder.Services.AddScoped<AdminApiClient>();
builder.Services.AddScoped<EmployeeApiClient>();
builder.Services.AddScoped<IAttendanceApiClient, AttendanceApiClient>();
builder.Services.AddScoped<ILeaveApiClient, LeaveApiClient>();
builder.Services.AddScoped<IPayrollApiClient, PayrollApiClient>();

await builder.Build().RunAsync();
