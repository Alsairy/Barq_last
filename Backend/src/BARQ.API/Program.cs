using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
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
using BARQ.Infrastructure.Services;
using BARQ.Core.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<AuthCookieOptions>(builder.Configuration.GetSection("Auth:Cookie"));

var allowed = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(o =>
{
    o.AddPolicy("Default", p => p
        .WithOrigins(allowed)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "BARQ API", Version = "v1" });
});

builder.Services.AddDbContext<BarqDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
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

var jwtSection = builder.Configuration.GetSection("Jwt");
var issuer = jwtSection["Issuer"];
var audience = jwtSection["Audience"];
var key = jwtSection["Key"]; // If using KMS/SecretManager, resolve here

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(key ?? "")),
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var cookieOpts = ctx.HttpContext.RequestServices.GetRequiredService<IOptions<AuthCookieOptions>>().Value;
                if (cookieOpts.Enabled &&
                    ctx.Request.Cookies.TryGetValue(cookieOpts.Name ?? "__Host-Auth", out var cookieToken) &&
                    !string.IsNullOrWhiteSpace(cookieToken))
                {
                    ctx.Token = cookieToken;
                    return System.Threading.Tasks.Task.CompletedTask;
                }

                var h = ctx.Request.Headers["Authorization"].ToString();
                if (h?.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) == true)
                    ctx.Token = h.Substring("Bearer ".Length).Trim();

                return System.Threading.Tasks.Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    
    options.AddPolicy("RequireAdministratorRole", policy => policy.RequireRole("Administrator"));
    options.AddPolicy("RequireManagerRole", policy => policy.RequireRole("Manager", "Administrator"));
    options.AddPolicy("RequireUserRole", policy => policy.RequireRole("User", "Manager", "Administrator"));
});

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
builder.Services.AddScoped<BARQ.Application.Interfaces.IAntiVirusService, BARQ.Application.Services.MockAntiVirusService>();
builder.Services.AddScoped<BARQ.Application.Interfaces.IAuditReportService, BARQ.Application.Services.AuditReportService>();
builder.Services.AddScoped<BARQ.Application.Interfaces.IBillingService, BARQ.Application.Services.BillingService>();
builder.Services.AddScoped<BARQ.Application.Interfaces.IQuotaMiddleware, BARQ.Application.Services.QuotaMiddleware>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantProvider, TenantProvider>();
builder.Services.AddScoped<ISlaService, SlaService>();
builder.Services.AddScoped<IEscalationService, EscalationService>();
builder.Services.AddHostedService<SlaMonitorWorker>();

builder.Services.AddMemoryCache();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<BarqDbContext>();

var app = builder.Build();

app.Use(async (ctx, next) =>
{
    if (!ctx.Request.Cookies.ContainsKey("XSRF-TOKEN"))
    {
        ctx.Response.Cookies.Append("XSRF-TOKEN", Guid.NewGuid().ToString("N"), new CookieOptions
        {
            SameSite = SameSiteMode.None,
            Secure = true,
            Path = "/"
        });
    }
    await next();
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.Use(async (ctx, next) =>
    {
        var p = ctx.Request.Path.Value ?? string.Empty;
        if ((p.StartsWith("/swagger") || p.StartsWith("/api-docs")) && !(ctx.User?.Identity?.IsAuthenticated ?? false))
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }
        await next();
    });
    app.UseSwagger();
    app.UseSwaggerUI();
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

app.Run();

public sealed class AuthCookieOptions
{
    public bool Enabled { get; set; } = true;
    public string Name { get; set; } = "__Host-Auth";
    public string? Domain { get; set; }  // usually omit for __Host- cookies
    public string Path { get; set; } = "/";
    public string SameSite { get; set; } = "None"; // enforced via CookieOptions
    public bool Secure { get; set; } = true;
}
