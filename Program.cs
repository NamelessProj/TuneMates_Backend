using Isopoh.Cryptography.Argon2;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TuneMates_Backend.DataBase;
using TuneMates_Backend.Route;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(opts => opts.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

var api = app.MapGroup("/api");

api.MapUserRoutes();

api.MapGet("/", TestFunction);
api.MapGet("/connect", Test2Function);

async Task<IResult> TestFunction(AppDbContext db)
{
    string password = "password123";

    var user = new User
    {
        Username = "testuser",
        Email = "test3@mail.com",
        PasswordHash = Argon2.Hash(password),
    };
    db.Users.Add(user);
    await db.SaveChangesAsync();
    return TypedResults.Ok(user);
}

async Task<IResult> Test2Function()
{
    string user_password_from_db = "$argon2id$v=19$m=65536,t=3,p=1$YvAWdUoDV8YPXCGbRwNdsQ$ra8jldcMAJIbxTPTfG+PmWTUqAr43/POcTil/32gPMc";
    string password_attempt = "password123";

    bool verified = Argon2.Verify(user_password_from_db, password_attempt);

    if (verified)
    {
        return TypedResults.Ok("Password is correct!");
    }
    else
    {
        return TypedResults.Unauthorized();
    }
}

// Launch the application.
app.Run();