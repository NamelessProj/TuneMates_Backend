using Microsoft.Extensions.Options;

namespace TuneMates_Backend.Infrastructure.Cors
{
    public static class CorsExtensions
    {
        /// <summary>
        /// Adds CORS services to the IServiceCollection.
        /// </summary>
        /// <param name="services">The IServiceCollection to add services to.</param>
        /// <param name="config">The IConfiguration to bind CORS settings from.</param>
        /// <returns>A configured IServiceCollection.</returns>
        /// <exception cref="InvalidOperationException">Thrown when AllowCredentials is true but no AllowedOrigins are specified.</exception>
        public static IServiceCollection AddAppCors(
            this IServiceCollection services,
            IConfiguration config)
        {
            services.Configure<CorsSettings>(config.GetSection("Cors"));

            services.AddCors(opts =>
            {
                opts.AddPolicy(CorsPolicies.Frontend, policy =>
                {
                    var settings = services.BuildServiceProvider()
                        .GetRequiredService<IOptions<CorsSettings>>().Value;

                    string[] allowedOrigins = settings.AllowedOrigins ?? Array.Empty<string>();
                    bool allowCredentials = settings.AllowCredentials;

                    if (allowCredentials)
                    {
                        if (allowedOrigins.Length == 0)
                            throw new InvalidOperationException("CORS configuration error: At least one allowed origin must be specified when AllowCredentials is true.");

                        policy.WithOrigins(allowedOrigins)
                                .AllowCredentials()
                                .AllowAnyHeader()
                                .AllowAnyMethod();
                    }
                    else
                    {
                        if (allowedOrigins.Length == 0)
                        {
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

            return services;
        }

        /// <summary>
        /// Uses the configured CORS policy in the application pipeline.
        /// </summary>
        /// <param name="app">The IApplicationBuilder to configure.</param>
        /// <returns>A configured IApplicationBuilder.</returns>
        public static IApplicationBuilder UseAppCors(this IApplicationBuilder app) => app.UseCors(CorsPolicies.Frontend);
    }
}