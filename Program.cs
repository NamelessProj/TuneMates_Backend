using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TuneMates_Backend.DataBase;
using TuneMates_Backend.Route;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(opts => opts.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

var api = app.MapGroup("/api");

api.MapUserRoutes();
api.MapRoomRoutes();

// Launch the application.
app.Run();