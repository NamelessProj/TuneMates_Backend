using Microsoft.EntityFrameworkCore;
using TuneMates_Backend.BackgroundServices;
using TuneMates_Backend.DataBase;
using TuneMates_Backend.Infrastructure.Auth;
using TuneMates_Backend.Infrastructure.Cors;
using TuneMates_Backend.Infrastructure.RateLimiting;
using TuneMates_Backend.Route;
using TuneMates_Backend.Utils;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAppJwtAuthentication(builder.Configuration);

builder.Services.AddDbContext<AppDbContext>(opts => opts.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Cron Jobs as background services
builder.Services.AddHostedService<TokenCleanupService>();
builder.Services.AddHostedService<ProposalCleanupService>();
builder.Services.AddHostedService<RoomCleanupService>();

builder.Services.AddMemoryCache();

// Configure CORS
builder.Services.AddAppCors(builder.Configuration);

// Configure Rate Limiting
builder.Services.AddAppRateLimiting(builder.Configuration);

var app = builder.Build();

// Activate CORS middleware
app.UseAppCors();

// Activate Rate Limiting middleware
app.UseAppRateLimiting();

// Activate JWT Authentication middleware
app.UseAppJwtAuthentication();

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