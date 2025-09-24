using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TuneMates_Backend.DataBase;
using TuneMates_Backend.Route;
using TuneMates_Backend.Utils;

var builder = WebApplication.CreateBuilder(args);

var jwtKey = builder.Configuration.GetValue<string>("Jwt:Key");
var jwtIssuer = builder.Configuration.GetValue<string>("Jwt:Issuer");
var jwtAudience = builder.Configuration.GetValue<string>("Jwt:Audience");

if (string.IsNullOrEmpty(jwtKey) || string.IsNullOrEmpty(jwtIssuer) || string.IsNullOrEmpty(jwtAudience))
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