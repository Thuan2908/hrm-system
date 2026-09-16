using System.Text;
using Hrm.Api.ErrorHandling;
using Hrm.Api.Health;
using Hrm.Contracts;
using Hrm.Modules.Auth;
using Hrm.Modules.Auth.Application;
using Hrm.Modules.Auth.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddJsonFile(
        "appsettings.Development.Local.json",
        optional: true,
        reloadOnChange: true);
}

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddControllers();
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddPolicy("Frontend", policy =>
{
    if (allowedOrigins.Length > 0)
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    }
}));
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
var authentication = builder.Services.AddAuthentication();
if (jwtOptions.SigningKey.Length >= 32)
{
    authentication.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            RoleClaimType = System.Security.Claims.ClaimTypes.Role,
            NameClaimType = System.Security.Claims.ClaimTypes.Name
        };
    });
}
builder.Services.AddAuthorization(options =>
{
    foreach (var permission in PermissionCodes.All)
    {
        options.AddPolicy(permission, policy => policy.RequireAssertion(context =>
            context.User.IsInRole("ADMIN") || context.User.HasClaim("permission", permission)));
    }
});
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddAuthModule(builder.Configuration);

var healthChecks = builder.Services
    .AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"]);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    healthChecks.AddCheck<DatabaseConfigurationHealthCheck>("database", tags: ["ready"]);
}
else
{
    healthChecks.AddNpgSql(
        connectionString,
        name: "database",
        tags: ["ready"]);
    healthChecks.AddCheck<AuthSchemaHealthCheck>("auth-schema", tags: ["ready"]);
}

var otlpEndpoint = builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];

builder.Services
    .AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation();

        if (Uri.TryCreate(otlpEndpoint, UriKind.Absolute, out var endpoint))
        {
            tracing.AddOtlpExporter(options => options.Endpoint = endpoint);
        }
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation();

        if (Uri.TryCreate(otlpEndpoint, UriKind.Absolute, out var endpoint))
        {
            metrics.AddOtlpExporter(options => options.Endpoint = endpoint);
        }
    });

var app = builder.Build();

if (app.Configuration.GetValue<bool>("Database:InitializeAuthSupportSchema"))
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<IAuthDatabaseInitializer>()
        .InitializeAsync(CancellationToken.None);
}

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks(
    "/health/live",
    HealthResponseWriter.CreateOptions(registration => registration.Tags.Contains("live")));
app.MapHealthChecks(
    "/health/ready",
    HealthResponseWriter.CreateOptions(registration => registration.Tags.Contains("ready")));
app.MapControllers();

app.Run();

public partial class Program;
