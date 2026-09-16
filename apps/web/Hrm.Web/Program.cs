using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Hrm.Web;
using Hrm.Web.Authorization;
using Hrm.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? builder.HostEnvironment.BaseAddress;
builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });
builder.Services.AddScoped<ISystemApiClient, SystemApiClient>();
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<BrowserSessionStore>();
builder.Services.AddScoped<AppAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(provider =>
    provider.GetRequiredService<AppAuthenticationStateProvider>());
builder.Services.AddScoped<IAuthApiClient, AuthApiClient>();
builder.Services.AddScoped<AdminApiClient>();
builder.Services.AddScoped<IAttendanceApiClient, AttendanceApiClient>();

await builder.Build().RunAsync();
