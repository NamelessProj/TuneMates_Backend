using Microsoft.EntityFrameworkCore;
using TuneMates_Backend.BackgroundServices;
using TuneMates_Backend.DataBase;
using TuneMates_Backend.Infrastructure.Auth;
using TuneMates_Backend.Infrastructure.Cors;
using TuneMates_Backend.Infrastructure.RateLimiting;
using TuneMates_Backend.Route;

var builder = WebApplication.CreateBuilder(args);

// Add JWT Authentication services
builder.Services.AddAppJwtAuthentication(builder.Configuration);

// Add DbContext with PostgreSQL provider
builder.Services.AddDbContext<AppDbContext>(opts => opts.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Cron Jobs as background services
builder.Services.AddHostedService<TokenCleanupService>();
builder.Services.AddHostedService<ProposalCleanupService>();
builder.Services.AddHostedService<RoomCleanupService>();

// Add Memory Cache
builder.Services.AddMemoryCache();

// Register JwtTokenService
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddSingleton<JwtTokenService>();

// Configure CORS
builder.Services.AddAppCors(builder.Configuration);

// Configure Rate Limiting
builder.Services.AddAppRateLimiting(builder.Configuration);

// Build the application.
var app = builder.Build();

// Activate CORS middleware
app.UseAppCors();

// Activate Rate Limiting middleware
app.UseAppRateLimiting();

// Activate JWT Authentication middleware
app.UseAppJwtAuthentication();

// Map API routes
var api = app.MapGroup("/api");

api.MapUserRoutes();
api.MapRoomRoutes();
api.MapSongRoutes();
api.MapSpotifyRoutes();

// Launch the application.
app.Run();