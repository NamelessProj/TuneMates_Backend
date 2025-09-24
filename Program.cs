using Microsoft.EntityFrameworkCore;
using TuneMates_Backend.DataBase;
using TuneMates_Backend.Route;
using TuneMates_Backend.Utils;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(opts => opts.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

var api = app.MapGroup("/api");

api.MapUserRoutes();
api.MapRoomRoutes();

api.MapGet("/test/{text}", TestEndpoint);

static async Task<IResult> TestEndpoint(HttpContext http, string text)
{
    IConfiguration cfg = http.RequestServices.GetRequiredService<IConfiguration>();
    EncryptionService encryptionService = new(cfg);
    string encrypted = encryptionService.Encrypt(text);
    string decrypted = encryptionService.Decrypt(encrypted);
    return TypedResults.Ok(new { original = text, encrypted, decrypted });
}

// Launch the application.
app.Run();