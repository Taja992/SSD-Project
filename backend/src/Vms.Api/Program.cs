using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Vms.Api.Api;
using Vms.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

var authority = builder.Configuration["Keycloak:Authority"]
    ?? throw new InvalidOperationException("Keycloak:Authority is not configured.");
var audience = builder.Configuration["Keycloak:Audience"]
    ?? throw new InvalidOperationException("Keycloak:Audience is not configured.");
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = authority;
        options.Audience = audience;
        options.RequireHttpsMetadata =
            builder.Configuration.GetValue("Keycloak:RequireHttpsMetadata", true);

        options.TokenValidationParameters = new TokenValidationParameters
        {
            // T-API-01: every one of these must be on. The signature check is
            // what makes the rest trustworthy.
            ValidateIssuer           = true,
            ValidIssuer              = authority,
            ValidateAudience         = true,
            ValidAudience            = audience,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ClockSkew                = TimeSpan.FromSeconds(30),

            // T-API-01: pin the algorithm. Without this a token claiming
            // "alg": "none" — or an HS256 token signed with the PUBLIC key —
            // can be accepted. This single line closes both attacks.
            ValidAlgorithms = [SecurityAlgorithms.RsaSha256],

            // Keycloak nests roles under realm_access.roles, so the default
            // role claim lookup finds nothing. Map it explicitly.
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = "preferred_username"
        };

        options.Events = new JwtBearerEvents
        {
            // Flatten realm_access.roles into standard role claims so
            // [Authorize(Roles = "PM")] and policies work normally.
            OnTokenValidated = context =>
            {
                var identity = context.Principal?.Identity as ClaimsIdentity;
                var realmAccess = context.Principal?.FindFirst("realm_access")?.Value;

                if (identity is not null && realmAccess is not null)
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(realmAccess);
                    if (doc.RootElement.TryGetProperty("roles", out var roles))
                    {
                        foreach (var role in roles.EnumerateArray())
                        {
                            var name = role.GetString();
                            if (!string.IsNullOrWhiteSpace(name))
                                identity.AddClaim(new Claim(ClaimTypes.Role, name));
                        }
                    }
                }
                return Task.CompletedTask;
            }
        };
    });

// Authorization policies. Roles come ONLY from the validated token — never
// from a header or body (T-API-05).
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("PmOnly",      p => p.RequireRole("PM"));
    options.AddPolicy("AnalystOrPm", p => p.RequireRole("Analyst", "PM"));
    options.AddPolicy("AnyStaff",    p => p.RequireRole("Developer", "Analyst", "PM", "Auditor"));

    // Deny by default: every endpoint needs authentication unless it opts out
    // with .AllowAnonymous(). Principle 1 in the project plan.
    options.FallbackPolicy = new Microsoft.AspNetCore.Authorization
        .AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// T-API-13: explicit origin allow-list. Never AllowAnyOrigin with credentials.
builder.Services.AddCors(options =>
{
    options.AddPolicy("spa", policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod());
});

// Enums cross the wire as STRINGS ("High"), not integers (3) — the same
// reasoning as storing them as strings in the database. A number in a request
// body is a magic value whose meaning silently changes if the enum is ever
// reordered, and the API already RETURNS strings, so accepting integers would
// make it asymmetric: read "High", write 3.
//
// Note this does NOT weaken validation. An unknown string is rejected by the
// converter, and [EnumDataType] still rejects a defined-but-out-of-range value.
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(
        new System.Text.Json.Serialization.JsonStringEnumConverter());
});

builder.Services.AddOpenApi();
builder.Services.AddValidation();
builder.Services.AddVmsPersistence(builder.Configuration);
builder.Services.AddVmsApplication();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("spa");
app.UseAuthentication();   // order matters: authenticate...
app.UseAuthorization();    // ...then authorize.

// Public — the only anonymous endpoint for now.
app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }))
   .AllowAnonymous();

// Any authenticated staff member. Proves the token chain works.
app.MapGet("/api/me", (ClaimsPrincipal user) => Results.Ok(new
{
    name  = user.Identity?.Name,
    roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray()
})).RequireAuthorization("AnyStaff");

// PM only. This is the endpoint that proves 403 works.
app.MapGet("/api/admin/ping", () => Results.Ok(new { pong = true }))
   .RequireAuthorization("PmOnly");

app.MapFindingsApi();


app.Run();
