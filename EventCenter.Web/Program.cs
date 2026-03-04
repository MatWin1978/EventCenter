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
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.RateLimiting;

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
builder.Services.AddScoped<CompanyBookingService>();
builder.Services.AddScoped<ParticipantQueryService>();
builder.Services.AddScoped<ParticipantExportService>();
builder.Services.AddScoped<CompanyService>();
builder.Services.AddScoped<GuestooEventApiService>();

// Email service
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("Smtp"));
builder.Services.AddScoped<IEmailSender, MailKitEmailSender>();

// Calendar export service
builder.Services.AddSingleton<ICalendarExportService, IcalNetCalendarService>();

// Persist Data Protection keys so auth cookies survive app restarts
var keysDirectory = new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "..", ".data-protection-keys"));
keysDirectory.Create();
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(keysDirectory);

// Configure Authentication with Keycloak OIDC
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
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
                                claimsIdentity.AddClaim(new Claim("role", roleValue));
                            }
                        }
                    }
                }
            }
            return Task.CompletedTask;
        },
        OnRedirectToIdentityProviderForSignOut = async context =>
        {
            // Read id_token from the still-active cookie and set as id_token_hint
            var result = await context.HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            var idToken = result?.Properties?.GetTokenValue("id_token");
            if (!string.IsNullOrEmpty(idToken))
            {
                context.ProtocolMessage.IdTokenHint = idToken;
            }
        }
    };
})
.AddJwtBearer(options =>
{
    options.Authority = builder.Configuration["Keycloak:Authority"];
    options.RequireHttpsMetadata = builder.Configuration.GetValue<bool>("Keycloak:RequireHttpsMetadata");
    options.TokenValidationParameters = new TokenValidationParameters
    {
        NameClaimType = "preferred_username",
        RoleClaimType = "role",
        ValidateAudience = false  // Keycloak issues aud="account" by default; issuer+signature validation is sufficient
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("MaklerOnly", policy => policy.RequireRole("Makler"));
    options.AddPolicy("GuestooApi", policy =>
    {
        policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
        policy.RequireAuthenticatedUser();
    });
});

// Rate limiting for anonymous company booking endpoint (AUTH-03 security)
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter(policyName: "CompanyBooking", opt =>
    {
        opt.PermitLimit = 10;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 0;
    });

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsync(
            "Zu viele Anfragen. Bitte versuchen Sie es später erneut.",
            cancellationToken);
    };
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

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// Authentication endpoints
app.MapGet("/auth/challenge", async (HttpContext context, string returnUrl = "/") =>
{
    await context.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme,
        new AuthenticationProperties { RedirectUri = returnUrl });
});

app.MapGet("/auth/debug-tokens", async (HttpContext context) =>
{
    var result = await context.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    if (!result.Succeeded)
        return Results.Text("NOT authenticated: " + result.Failure?.Message);
    var tokens = result.Properties?.Items
        .Where(kv => kv.Key.StartsWith(".Token."))
        .Select(kv => $"{kv.Key} = {kv.Value?[..Math.Min(40, kv.Value.Length)]}...")
        .ToList() ?? [];
    return Results.Text("Tokens:\n" + string.Join("\n", tokens));
});

app.MapGet("/auth/signout", async (HttpContext context) =>
{
    // Read id_token before clearing the cookie
    var authResult = await context.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    var idToken = authResult?.Properties?.GetTokenValue("id_token");

    // Clear the local cookie
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

    // Build Keycloak logout URL directly — bypasses OIDC handler issues with id_token_hint
    var authority = app.Configuration["Keycloak:Authority"]!.TrimEnd('/');
    var postLogoutUri = Uri.EscapeDataString($"{context.Request.Scheme}://{context.Request.Host}/");
    var logoutUrl = $"{authority}/protocol/openid-connect/logout?post_logout_redirect_uri={postLogoutUri}";
    if (!string.IsNullOrEmpty(idToken))
        logoutUrl += $"&id_token_hint={Uri.EscapeDataString(idToken)}";

    context.Response.Redirect(logoutUrl);
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
    bool view,
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

    // view=true → inline (browser öffnet PDF im Tab); default → attachment (Download)
    return view
        ? Results.File(physicalPath, contentType: contentType)
        : Results.File(physicalPath, contentType: contentType, fileDownloadName: sanitizedPath);
}).RequireAuthorization("MaklerOnly");

// Guestoo-compatible events API — returns published, non-expired events as JSON
// Auth: Keycloak Bearer token; optional cPAgency header is accepted but not used (no agency filter in EventCenter)
app.MapGet("/api/cs/guestooevents", async (
    GuestooEventApiService guestooService) =>
{
    var events = await guestooService.GetActiveEventsAsync();
    return Results.Ok(events);
}).RequireAuthorization("GuestooApi");

// Apply migrations automatically in Development environment
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<EventCenterDbContext>();

    // Apply pending migrations
    dbContext.Database.Migrate();
}

app.Run();
