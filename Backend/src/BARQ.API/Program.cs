using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Identity;
using System.Text;
using BARQ.Infrastructure.Data;
using BARQ.Core.Entities;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "default-key"))
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdministratorRole", policy => policy.RequireRole("Administrator"));
    options.AddPolicy("RequireManagerRole", policy => policy.RequireRole("Manager", "Administrator"));
    options.AddPolicy("RequireUserRole", policy => policy.RequireRole("User", "Manager", "Administrator"));
});

builder.Services.AddScoped<BARQ.Application.Services.RecycleBin.IRecycleBinService, BARQ.Application.Services.RecycleBin.RecycleBinService>();

builder.Services.AddScoped<BARQ.Application.Interfaces.INotificationService, BARQ.Application.Services.NotificationService>();
builder.Services.AddScoped<BARQ.Application.Interfaces.INotificationPreferenceService, BARQ.Application.Services.NotificationPreferenceService>();
builder.Services.AddScoped<BARQ.Application.Interfaces.IEmailService, BARQ.Application.Services.EmailService>();
builder.Services.AddScoped<BARQ.Application.Interfaces.IFileAttachmentService, BARQ.Application.Services.FileAttachmentService>();
builder.Services.AddScoped<BARQ.Application.Interfaces.IFileStorageService, BARQ.Application.Services.LocalFileStorageService>();
builder.Services.AddScoped<BARQ.Application.Interfaces.IAntiVirusService, BARQ.Application.Services.MockAntiVirusService>();
builder.Services.AddScoped<BARQ.Application.Interfaces.IAuditReportService, BARQ.Application.Services.AuditReportService>();
builder.Services.AddScoped<BARQ.Application.Interfaces.IBillingService, BARQ.Application.Services.BillingService>();
builder.Services.AddScoped<BARQ.Application.Interfaces.IQuotaMiddleware, BARQ.Application.Services.QuotaMiddleware>();

builder.Services.AddScoped<BARQ.Application.Interfaces.ITranslationService, BARQ.Application.Services.TranslationService>();
builder.Services.AddScoped<BARQ.Application.Interfaces.ILanguageService, BARQ.Application.Services.LanguageService>();
builder.Services.AddScoped<BARQ.Application.Interfaces.IAccessibilityService, BARQ.Application.Services.AccessibilityService>();
builder.Services.AddScoped<BARQ.Application.Interfaces.IUserLanguagePreferenceService, BARQ.Application.Services.UserLanguagePreferenceService>();

builder.Services.AddMemoryCache();

builder.Services.AddCors(options =>
{
    options.AddPolicy("SecurePolicy", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddHealthChecks()
    .AddDbContextCheck<BarqDbContext>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("SecurePolicy");
app.UseAuthentication();
app.UseAuthorization();

if (!app.Environment.IsDevelopment())
{
    app.Use(async (ctx, next) =>
    {
        var path = ctx.Request.Path.Value ?? string.Empty;
        if ((path.StartsWith("/swagger") || path.StartsWith("/api-docs")) && !(ctx.User?.Identity?.IsAuthenticated ?? false))
        {
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }
        await next();
    });
}

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
