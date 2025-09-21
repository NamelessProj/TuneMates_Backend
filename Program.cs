using Microsoft.EntityFrameworkCore;
using TuneMates_Backend.DataBase;
using TuneMates_Backend.Utils;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(opts => opts.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Ensure the database is created.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

var api = app.MapGroup("/api");

api.MapGet("/", TestFunction);

async Task<IResult> TestFunction(AppDbContext db)
{
    string password = "password123";
    var passwordService = new PasswordService();


    var user = new User
    {
        Username = "testuser",
        Email = "test2@mail.com",
        PasswordHash = passwordService.Hash(password),
    };
    db.Users.Add(user);
    await db.SaveChangesAsync();
    return TypedResults.Ok(user);
}

// Launch the application.
app.Run();