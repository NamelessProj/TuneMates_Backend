using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TuneMates_Backend.BackgroundServices;
using TuneMates_Backend.DataBase;
using TuneMates_Backend.Infrastructure.RateLimiting;
using TuneMates_Backend.Route;
using TuneMates_Backend.Utils;

var builder = WebApplication.CreateBuilder(args);

var jwtKey = builder.Configuration.GetValue<string>("Jwt:Key");
var jwtIssuer = builder.Configuration.GetValue<string>("Jwt:Issuer");
var jwtAudience = builder.Configuration.GetValue<string>("Jwt:Audience");

if (string.IsNullOrWhiteSpace(jwtKey) || string.IsNullOrWhiteSpace(jwtIssuer) || string.IsNullOrWhiteSpace(jwtAudience))
    throw new ArgumentNullException("JWT configuration is missing or incomplete.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new()
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddDbContext<AppDbContext>(opts => opts.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Cron Jobs as background services
builder.Services.AddHostedService<TokenCleanupService>();
builder.Services.AddHostedService<ProposalCleanupService>();
builder.Services.AddHostedService<RoomCleanupService>();

builder.Services.AddMemoryCache();

// Configure CORS to allow requests from the frontend application
string[] allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
bool allowCredentials = builder.Configuration.GetValue<bool?>("Cors:AllowCrendentials") ?? false;

builder.Services.AddCors(opts =>
{
    opts.AddPolicy("Frontend", policy =>
    {
        if (allowCredentials)
        {
            if (allowedOrigins.Length == 0)
                throw new InvalidOperationException("CORS is configured to allow credentials, but no allowed origins are specified.");

            policy.WithOrigins(allowedOrigins)
                  .AllowCredentials()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        }
        else
        {
            if (allowedOrigins.Length == 0)
            {
                // No credentials -> allow any origin
                policy.AllowAnyOrigin()
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            }
            else
            {
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            }
        }
    });
});

// Configure Rate Limiting
builder.Services.AddAppRateLimiting(builder.Configuration);

var app = builder.Build();

// Activate CORS middleware
app.UseCors("Frontend");

// Activate Rate Limiting middleware
app.UseAppRateLimiting();

app.UseAuthentication();
app.UseAuthorization();

var api = app.MapGroup("/api");

api.MapUserRoutes();
api.MapRoomRoutes();
api.MapSongRoutes();
api.MapSpotifyRoutes();

api.MapPost("/test/{text}", TestEndpoint).RequireAuthorization();

static async Task<IResult> TestEndpoint(HttpContext http, string text)
{
    // Getting the user ID from the JWT token
    var userId = HelpMethods.GetUserIdFromJwtClaims(http);

    return TypedResults.Ok(new
    {
        Message = "Test endpoint is working!",
        Text = text,
        UserId = userId != null ? userId : 0
    });
}

// Launch the application.
app.Run();