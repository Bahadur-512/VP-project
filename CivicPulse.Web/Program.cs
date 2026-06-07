using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using CivicPulse.Core.Interfaces;
using CivicPulse.Core.Entities;
using CivicPulse.Core.Enums;
using CivicPulse.Core.Helpers;
using CivicPulse.Infrastructure.Data;
using CivicPulse.Infrastructure.Repositories;
using CivicPulse.Core.DTOs;
using CivicPulse.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
        options.Cookie.Name = "CivicPulse.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.AccessDeniedPath = "/";
    });

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
builder.Services.AddScoped<LocalizationService>();
builder.Services.AddScoped<CategorizationEngine>();
builder.Services.AddHttpClient<AiCategorizationService>();
builder.Services.AddScoped<AiCategorizationService>();
builder.Services.AddHostedService<SlaMonitoringService>();
builder.Services.AddHostedService<DatabaseSeedService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();

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

app.MapPost("/api/auth/login", async (HttpContext context, IUserService userService) =>
{
    var form = await context.Request.ReadFormAsync();
    var email = form["Email"].FirstOrDefault() ?? "";
    var password = form["Password"].FirstOrDefault() ?? "";
    var rememberMe = form["RememberMe"].FirstOrDefault() == "true";

    try
    {
        var user = await userService.LoginAsync(new LoginDto { Email = email, Password = password, RememberMe = rememberMe });
        var principal = CreatePrincipal(user, "login");
        await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
            new AuthenticationProperties { IsPersistent = rememberMe, ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7) });
        var role = principal.FindFirst("role")?.Value;
        var redirect = role switch
        {
            "Admin" => "/admin/dashboard",
            "Citizen" => "/citizen/dashboard",
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
        var user = await userService.RegisterAsync(new RegisterDto
        {
            FullName = fullName, Email = email, PhoneNumber = phoneNumber,
            Password = password, ConfirmPassword = confirmPassword
        });
        var principal = CreatePrincipal(user, "register");
        await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
            new AuthenticationProperties { IsPersistent = false, ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7) });
        return Results.Redirect("/citizen/dashboard");
    }
    catch (InvalidOperationException ex)
    {
        return Results.Redirect($"/register?error={Uri.EscapeDataString(ex.Message)}");
    }
});

app.MapGet("/api/auth/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
});

static ClaimsPrincipal CreatePrincipal(UserDto user, string method)
{
    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new(ClaimTypes.Email, user.Email),
        new(ClaimTypes.Name, user.FullName),
        new("role", user.Role.ToString()),
        new("language", user.PreferredLanguage),
        new("userId", user.Id.ToString())
    };
    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    return new ClaimsPrincipal(identity);
}

app.Run();
