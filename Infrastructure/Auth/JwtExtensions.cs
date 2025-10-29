using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace TuneMates_Backend.Infrastructure.Auth
{
    public static class JwtExtensions
    {
        /// <summary>
        /// Adds JWT authentication services to the IServiceCollection.
        /// </summary>
        /// <param name="services">The IServiceCollection to add services to.</param>
        /// <param name="config">The IConfiguration to bind JWT settings from.</param>
        /// <returns>A configured IServiceCollection.</returns>
        /// <exception cref="ArgumentNullException">Thrown when JWT configuration is missing or incomplete.</exception>
        public static IServiceCollection AddAppJwtAuthentication(
            this IServiceCollection services,
            IConfiguration config)
        {
            services.Configure<JwtSettings>(config.GetSection("Jwt"));

            var settings = config.GetSection("Jwt").Get<JwtSettings>()
                ?? throw new ArgumentNullException("JWT configuration is missing.");

            if (string.IsNullOrWhiteSpace(settings.Key) ||
                string.IsNullOrWhiteSpace(settings.Issuer) ||
                string.IsNullOrWhiteSpace(settings.Audience))
            {
                throw new ArgumentNullException("JWT configuration is missing or incomplete.");
            }

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(opts =>
                {
                    opts.TokenValidationParameters = new()
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = settings.Issuer,
                        ValidAudience = settings.Audience,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Key)),
                        ClockSkew = TimeSpan.FromSeconds(30) // Allow a small clock skew for token expiration
                    };
                });

            services.AddAuthorization();

            return services;
        }

        /// <summary>
        /// Activates JWT authentication middleware in the application pipeline.
        /// </summary>
        /// <param name="app">The IApplicationBuilder to configure.</param>
        /// <returns>A configured IApplicationBuilder.</returns>
        public static IApplicationBuilder UseAppJwtAuthentication(this IApplicationBuilder app)
        {
            app.UseAuthentication();
            app.UseAuthorization();
            return app;
        }
    }
}