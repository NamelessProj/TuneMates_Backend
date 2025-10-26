using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TuneMates_Backend.BackgroundServices;
using TuneMates_Backend.DataBase;
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
builder.Services.AddCors(opts =>
{
    opts.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins(
            "http://localhost:5173",
            "https://localhost:5173",
            "http://127.0.0.1:5173",
            "https://127.0.0.1:5173")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});

var app = builder.Build();

// Activate CORS middleware
app.UseCors("Frontend");

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