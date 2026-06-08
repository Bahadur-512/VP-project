using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using CivicPulse.Core.Interfaces;
using CivicPulse.Core.Entities;
using CivicPulse.Core.Enums;
using CivicPulse.Core.Helpers;
using CivicPulse.Core.Models;
using CivicPulse.Infrastructure.Data;
using CivicPulse.Infrastructure.Repositories;
using CivicPulse.Core.DTOs;
using CivicPulse.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── Bind JWT Settings ──
builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("JwtSettings"));

var jwtSettings = builder.Configuration
    .GetSection("JwtSettings").Get<JwtSettings>()!;

// ── JWT Authentication ──
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.MapInboundClaims = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
        ValidateIssuer   = true,
        ValidIssuer      = jwtSettings.Issuer,
        ValidateAudience = true,
        ValidAudience    = jwtSettings.Audience,
        ValidateLifetime = true,
        ClockSkew        = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var token = context.Request.Cookies["civic_jwt"];
            if (!string.IsNullOrEmpty(token))
                context.Token = token;
            return Task.CompletedTask;
        }
    };
});

// ── Authorization Policies ──
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", p => p.RequireClaim("role", "Admin"));
    options.AddPolicy("CitizenOnly", p => p.RequireClaim("role", "Citizen"));
    options.AddPolicy("AuthenticatedUser", p => p.RequireAuthenticatedUser());
});

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.Configure<SmtpSettings>(
    builder.Configuration.GetSection("SmtpSettings"));
builder.Services.AddScoped<IEmailNotificationService, EmailNotificationService>();
builder.Services.AddScoped<IComplaintService, ComplaintService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddScoped<IFeedbackService, FeedbackService>();
builder.Services.AddScoped<IAttachmentService, AttachmentService>();
builder.Services.AddScoped<ISlaService, SlaService>();

builder.Services.AddScoped<CategorizationEngine>();
builder.Services.AddHttpClient<AiCategorizationService>();
builder.Services.AddScoped<AiCategorizationService>();
builder.Services.AddHostedService<SlaMonitoringService>();
builder.Services.AddHostedService<DatabaseSeedService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();

// ── JWT Services ──
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IJwtAuthService, JwtAuthService>();

builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 52428800;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.MapPost("/api/auth/login", async (HttpContext context, IUserService userService, IJwtTokenService jwtService) =>
{
    var form = await context.Request.ReadFormAsync();
    var email = form["Email"].FirstOrDefault() ?? "";
    var password = form["Password"].FirstOrDefault() ?? "";
    var rememberMe = form["RememberMe"].FirstOrDefault() == "true";

    try
    {
        var user = await userService.LoginAsync(new LoginDto { Email = email, Password = password, RememberMe = rememberMe });

        // Generate JWT and store in HttpOnly cookie
        var token = jwtService.GenerateToken(user);
        var expiry = rememberMe ? DateTimeOffset.UtcNow.AddDays(7) : DateTimeOffset.UtcNow.AddHours(jwtSettings.ExpiryHours);
        context.Response.Cookies.Append("civic_jwt", token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = expiry,
            Path = "/"
        });

        var redirect = user.Role switch
        {
            UserRole.Admin => "/admin/dashboard",
            UserRole.Citizen => "/citizen/dashboard",
            _ => "/dashboard"
        };
        return Results.Redirect(redirect);
    }
    catch (UnauthorizedAccessException ex)
    {
        return Results.Redirect($"/login?error={Uri.EscapeDataString(ex.Message)}");
    }
});

app.MapPost("/api/auth/register", async (HttpContext context, IUserService userService) =>
{
    var form = await context.Request.ReadFormAsync();
    var fullName = form["FullName"].FirstOrDefault() ?? "";
    var email = form["Email"].FirstOrDefault() ?? "";
    var phoneNumber = form["PhoneNumber"].FirstOrDefault() ?? "";
    var password = form["Password"].FirstOrDefault() ?? "";
    var confirmPassword = form["ConfirmPassword"].FirstOrDefault() ?? "";

    try
    {
        await userService.RegisterAsync(new RegisterDto
        {
            FullName = fullName, Email = email, PhoneNumber = phoneNumber,
            Password = password, ConfirmPassword = confirmPassword
        });
        return Results.Redirect("/login?registered=true");
    }
    catch (InvalidOperationException ex)
    {
        var msg = ex.Message == "Email already registered."
            ? "User is already registered. You can log in."
            : ex.Message;
        return Results.Redirect($"/login?error={Uri.EscapeDataString(msg)}");
    }
});

app.MapGet("/api/auth/logout", async (HttpContext context, IJwtAuthService authService) =>
{
    await authService.SignOutAsync();
    return Results.Redirect("/login");
});

app.Run();
