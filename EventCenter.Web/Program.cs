using EventCenter.Web.Components;
using EventCenter.Web.Domain;
using EventCenter.Web.Infrastructure.Authentication;
using EventCenter.Web.Infrastructure.Calendar;
using EventCenter.Web.Infrastructure.Email;
using EventCenter.Web.Services;
using EventCenter.Web.Validators;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Configure DbContext with SQL Server
builder.Services.AddDbContext<EventCenterDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
            sqlOptions.CommandTimeout(30);
        })
    .EnableSensitiveDataLogging(builder.Environment.IsDevelopment())
    .EnableDetailedErrors(builder.Environment.IsDevelopment()));

// Configure FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<EventValidator>();

// Register application services
builder.Services.AddScoped<EventService>();
builder.Services.AddScoped<RegistrationService>();
builder.Services.AddScoped<CompanyInvitationService>();

// Email service
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("Smtp"));
builder.Services.AddScoped<IEmailSender, MailKitEmailSender>();

// Calendar export service
builder.Services.AddSingleton<ICalendarExportService, IcalNetCalendarService>();

// Configure Authentication with Keycloak OIDC
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
})
.AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
{
    options.Authority = builder.Configuration["Keycloak:Authority"];
    options.ClientId = builder.Configuration["Keycloak:ClientId"];
    options.ClientSecret = builder.Configuration["Keycloak:ClientSecret"];
    options.ResponseType = "code";
    options.SaveTokens = true;
    options.GetClaimsFromUserInfoEndpoint = true;
    options.RequireHttpsMetadata = builder.Configuration.GetValue<bool>("Keycloak:RequireHttpsMetadata");

    options.TokenValidationParameters = new TokenValidationParameters
    {
        NameClaimType = "preferred_username",
        RoleClaimType = "role"
    };

    options.Events = new OpenIdConnectEvents
    {
        OnTokenValidated = context =>
        {
            var claimsIdentity = context.Principal?.Identity as ClaimsIdentity;
            if (claimsIdentity != null)
            {
                // Extract realm roles from Keycloak token
                var realmAccessClaim = context.Principal?.FindFirst("realm_access");
                if (realmAccessClaim != null)
                {
                    var realmAccess = JsonSerializer.Deserialize<JsonElement>(realmAccessClaim.Value);
                    if (realmAccess.TryGetProperty("roles", out var roles))
                    {
                        foreach (var role in roles.EnumerateArray())
                        {
                            var roleValue = role.GetString();
                            if (!string.IsNullOrEmpty(roleValue))
                            {
                                claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, roleValue));
                            }
                        }
                    }
                }
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("MaklerOnly", policy => policy.RequireRole("Makler"));
});

// Register authentication state provider with 30-minute revalidation
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthStateProvider>();
builder.Services.AddScoped<RevalidatingServerAuthenticationStateProvider, IdentityRevalidatingAuthStateProvider>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

// Authentication endpoints
app.MapGet("/auth/challenge", async (HttpContext context, string returnUrl = "/") =>
{
    await context.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme,
        new AuthenticationProperties { RedirectUri = returnUrl });
});

app.MapGet("/auth/signout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    await context.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Minimal API endpoints for calendar and document downloads
app.MapGet("/api/events/{eventId:int}/calendar", async (
    int eventId,
    EventService eventService,
    ICalendarExportService calendarService) =>
{
    var evt = await eventService.GetEventByIdAsync(eventId);
    if (evt == null || !evt.IsPublished)
        return Results.NotFound();

    var icsBytes = calendarService.GenerateEventCalendar(evt);
    return Results.File(icsBytes, contentType: "text/calendar", fileDownloadName: $"event-{evt.Id}.ics");
}).RequireAuthorization("MaklerOnly");

app.MapGet("/api/events/{eventId:int}/documents/{*filePath}", async (
    int eventId,
    string filePath,
    EventService eventService,
    IWebHostEnvironment env) =>
{
    var evt = await eventService.GetEventByIdAsync(eventId);
    if (evt == null || !evt.IsPublished)
        return Results.NotFound();

    // Path traversal protection
    var sanitizedPath = Path.GetFileName(filePath);
    var expectedPrefix = $"/uploads/events/{eventId}/";
    var fullRelativePath = $"{expectedPrefix}{sanitizedPath}";

    if (!evt.DocumentPaths.Contains(fullRelativePath))
        return Results.NotFound();

    var physicalPath = Path.Combine(env.WebRootPath, "uploads", "events", eventId.ToString(), sanitizedPath);
    if (!File.Exists(physicalPath))
        return Results.NotFound();

    var contentType = Path.GetExtension(physicalPath).ToLowerInvariant() switch
    {
        ".pdf" => "application/pdf",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        _ => "application/octet-stream"
    };

    return Results.File(physicalPath, contentType: contentType, fileDownloadName: sanitizedPath);
}).RequireAuthorization("MaklerOnly");

// Apply migrations automatically in Development environment
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<EventCenterDbContext>();

    // Apply pending migrations
    dbContext.Database.Migrate();
}

app.Run();
