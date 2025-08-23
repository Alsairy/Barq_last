using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using BARQ.Infrastructure.Data;
using BARQ.Core.Entities;
using BARQ.Application.Interfaces;
using BARQ.Application.Services;
using BARQ.Application.Services.Workflow;
using BARQ.Core.Services;
using BARQ.Infrastructure.Services;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithProcessId()
    .Enrich.WithThreadId()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.File("logs/barq-.log", 
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.File("logs/security-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 90,
        restrictedToMinimumLevel: LogEventLevel.Warning,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] SECURITY: {Message:lj} {Properties:j}{NewLine}{Exception}")
    .Filter.ByExcluding(logEvent => 
        logEvent.MessageTemplate.Text.Contains("password", StringComparison.OrdinalIgnoreCase) ||
        logEvent.MessageTemplate.Text.Contains("token", StringComparison.OrdinalIgnoreCase) ||
        logEvent.MessageTemplate.Text.Contains("secret", StringComparison.OrdinalIgnoreCase))
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog();
var allowed = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(o => o.AddPolicy("Default", p => p.WithOrigins(allowed).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => c.SwaggerDoc("v1", new OpenApiInfo { Title = "BARQ API", Version = "v1" }));

builder.Services.AddDbContext<BarqDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, Role>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
    options.Lockout.MaxFailedAccessAttempts = 5;
})
.AddEntityFrameworkStores<BarqDbContext>()
.AddDefaultTokenProviders();

builder.Services.Configure<AuthCookieOptions>(builder.Configuration.GetSection("Auth:Cookie"));
var jwt = builder.Configuration.GetSection("Jwt");
var issuer = jwt["Issuer"]; var audience = jwt["Audience"]; var keyB64 = jwt["Key"];

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true, ValidateIssuerSigningKey = true,
            ValidIssuer = issuer, ValidAudience = audience,
            IssuerSigningKey = string.IsNullOrWhiteSpace(keyB64)
                ? new SymmetricSecurityKey(new byte[32])
                : new SymmetricSecurityKey(Convert.FromBase64String(keyB64)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var opt = ctx.HttpContext.RequestServices.GetRequiredService<IOptions<AuthCookieOptions>>().Value;
                if (opt.Enabled && ctx.Request.Cookies.TryGetValue(opt.Name ?? "__Host-Auth", out var t) && !string.IsNullOrWhiteSpace(t))
                { ctx.Token = t; return System.Threading.Tasks.Task.CompletedTask; }
                var h = ctx.Request.Headers["Authorization"].ToString();
                if (h?.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) == true)
                    ctx.Token = h.Substring("Bearer ".Length).Trim();
                return System.Threading.Tasks.Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(o => o.FallbackPolicy =
    new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

builder.Services.AddScoped<BARQ.Application.Services.RecycleBin.IRecycleBinService, BARQ.Application.Services.RecycleBin.RecycleBinService>();
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<BARQ.Application.Interfaces.ITranslationService, BARQ.Application.Services.TranslationService>();
builder.Services.AddScoped<BARQ.Application.Interfaces.ILanguageService, BARQ.Application.Services.LanguageService>();
builder.Services.AddScoped<BARQ.Application.Interfaces.IAccessibilityService, BARQ.Application.Services.AccessibilityService>();
builder.Services.AddScoped<BARQ.Application.Interfaces.IUserLanguagePreferenceService, BARQ.Application.Services.UserLanguagePreferenceService>();
builder.Services.AddScoped<BARQ.Application.Interfaces.INotificationService, BARQ.Application.Services.NotificationService>();
builder.Services.AddScoped<BARQ.Application.Interfaces.INotificationPreferenceService, BARQ.Application.Services.NotificationPreferenceService>();
builder.Services.AddScoped<BARQ.Application.Interfaces.IEmailService, BARQ.Application.Services.EmailService>();
builder.Services.AddScoped<BARQ.Application.Interfaces.IFileAttachmentService, BARQ.Application.Services.FileAttachmentService>();
builder.Services.AddScoped<BARQ.Application.Interfaces.IFileStorageService, BARQ.Application.Services.LocalFileStorageService>();
builder.Services.AddScoped<BARQ.Application.Interfaces.IAntiVirusService, BARQ.Application.Services.AntiVirusService>();
builder.Services.AddScoped<BARQ.Application.Interfaces.IAuditReportService, BARQ.Application.Services.AuditReportService>();
builder.Services.AddScoped<BARQ.Application.Interfaces.IBillingService, BARQ.Application.Services.BillingService>();
builder.Services.AddScoped<BARQ.Application.Interfaces.IQuotaMiddleware, BARQ.Application.Services.QuotaMiddleware>();
builder.Services.AddScoped<BARQ.Application.Interfaces.IFeatureFlagService, BARQ.Application.Services.FeatureFlagService>();
builder.Services.AddScoped<BARQ.Application.Interfaces.IImpersonationService, BARQ.Application.Services.ImpersonationService>();
builder.Services.AddScoped<BARQ.Application.Interfaces.ISystemHealthService, BARQ.Application.Services.SystemHealthService>();
builder.Services.AddScoped<BARQ.Application.Interfaces.ITenantStateService, BARQ.Application.Services.TenantStateService>();
builder.Services.AddSingleton<BackgroundJobService>();
builder.Services.AddSingleton<IBackgroundJobService>(sp => sp.GetRequiredService<BackgroundJobService>());
builder.Services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<BackgroundJobService>());
builder.Services.AddHttpContextAccessor(); // for TenantProvider
builder.Services.AddScoped<ITenantProvider, TenantProvider>();

builder.Services.Configure<FlowableOptions>(builder.Configuration.GetSection("Flowable"));
builder.Services.PostConfigure<FlowableOptions>(opt =>
{
    if (!string.IsNullOrWhiteSpace(opt.BaseUrl) && !opt.BaseUrl.EndsWith("/"))
        opt.BaseUrl += "/";
});
builder.Services.AddHttpClient("flowable", (sp, http) =>
{
    var opt = sp.GetRequiredService<IOptions<FlowableOptions>>().Value;
    http.BaseAddress = new Uri(opt.BaseUrl);
    http.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddHttpClient<IFlowableGateway, FlowableGateway>("flowable-gateway", (sp, http) =>
{
    var opt = sp.GetRequiredService<IOptions<FlowableOptions>>().Value;
    http.BaseAddress = new Uri(opt.BaseUrl);
    http.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddScoped<BARQ.Application.Interfaces.ITemplateValidationService, BARQ.Application.Services.TemplateValidationService>();

// builder.Services.AddHostedService<SlaMonitorHostedService>();
// builder.Services.AddScoped<ISlaCalculator, SlaCalculator>();
// builder.Services.AddScoped<IEscalationExecutor, EscalationExecutor>();

builder.Services.AddMemoryCache();
builder.Services.AddHealthChecks()
    .AddDbContextCheck<BarqDbContext>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<BarqDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<Role>>();
    
    try
    {
        await context.Database.MigrateAsync();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Database migration failed");
        throw;
    }

    var shouldSeed = app.Environment.IsDevelopment() ||
                     string.Equals(Environment.GetEnvironmentVariable("BARQ_SEED"), "true", StringComparison.OrdinalIgnoreCase);
    if (shouldSeed)
    {
        try
        {
            await DbSeeder.SeedAsync(context, userManager, roleManager);
        }
        catch (Exception ex)
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "Database seeding failed");
            throw;
        }
    }
}

app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].FirstOrDefault());
        if (httpContext.User.Identity?.IsAuthenticated == true)
        {
            diagnosticContext.Set("UserId", httpContext.User.FindFirst("sub")?.Value ?? httpContext.User.FindFirst("id")?.Value);
        }
    };
});

app.UseExceptionHandler();
app.UseStatusCodePages();
app.Use(async (ctx, next) =>
{
    if (!ctx.Request.Cookies.ContainsKey("XSRF-TOKEN"))
        ctx.Response.Cookies.Append("XSRF-TOKEN", Guid.NewGuid().ToString("N"),
            new CookieOptions { SameSite = SameSiteMode.None, Secure = true, Path = "/" });
    await next();
});

if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
else
{
    app.Use(async (ctx, next) =>
    {
        var p = ctx.Request.Path.Value ?? string.Empty;
        if ((p.StartsWith("/swagger") || p.StartsWith("/api-docs")) && !(ctx.User?.Identity?.IsAuthenticated ?? false))
        { ctx.Response.StatusCode = StatusCodes.Status401Unauthorized; return; }
        await next();
    });
    app.UseSwagger(); app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("Default");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(x => new
            {
                name = x.Key,
                status = x.Value.Status.ToString(),
                exception = x.Value.Exception?.Message,
                duration = x.Value.Duration.ToString()
            })
        };
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
    }
});

try
{
    Log.Information("Starting BARQ API application");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "BARQ API application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }

public sealed class AuthCookieOptions
{
    public bool Enabled { get; set; } = true;
    public string Name { get; set; } = "__Host-Auth";
    public string Path { get; set; } = "/";
    public bool Secure { get; set; } = true;
    public string SameSite { get; set; } = "None";
}
