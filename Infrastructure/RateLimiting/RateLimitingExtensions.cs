using Microsoft.Extensions.Options;
using System.Threading.RateLimiting;

namespace TuneMates_Backend.Infrastructure.RateLimiting
{
    public static class RateLimitingExtensions
    {
        /// <summary>
        /// Adds rate limiting services to the IServiceCollection.
        /// </summary>
        /// <param name="services">The IServiceCollection to add services to.</param>
        /// <param name="config">The IConfiguration to bind rate limiting settings from.</param>
        /// <returns>A configured IServiceCollection.</returns>
        public static IServiceCollection AddAppRateLimiting(
            this IServiceCollection services,
            IConfiguration config)
        {
            services.Configure<RateLimitingSettings>(config.GetSection("RateLimiting"));

            services.AddRateLimiter(opts =>
            {
                opts.OnRejected = async (ctx, token) =>
                {
                    ctx.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

                    // Try to get the Retry-After metadata if available
                    int? retryAfterSeconds = null;
                    if (ctx.Lease.TryGetMetadata(MetadataName.RetryAfter, out var ts) && ts is TimeSpan timeSpan)
                        retryAfterSeconds = (int)timeSpan.TotalSeconds;

                    await ctx.HttpContext.Response.WriteAsJsonAsync(new
                    {
                        error = "rate_limit_exceeded",
                        message = "Rate limit exceeded. Please try again later.",
                        retry_after_seconds = retryAfterSeconds
                    }, cancellationToken: token);
                };

                // Build rate limit policies from settings
                opts.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                opts.AddPolicy(RateLimitPolicies.SearchTight, http =>
                {
                    string key = GetPartitionKey(http);
                    var settings = http.RequestServices.GetRequiredService<IOptions<RateLimitingSettings>>().Value;

                    return RateLimitPartition.GetSlidingWindowLimiter(
                        key,
                        _ => new SlidingWindowRateLimiterOptions
                        {
                            PermitLimit = Math.Max(1, settings.SearchTightPerMinute),
                            Window = TimeSpan.FromMinutes(1),
                            SegmentsPerWindow = 6, // 10-second segments
                            QueueLimit = 0,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                        });
                });

                opts.AddPolicy(RateLimitPolicies.Mutations, http =>
                {
                    string key = GetPartitionKey(http);
                    var settings = http.RequestServices.GetRequiredService<IOptions<RateLimitingSettings>>().Value;

                    return RateLimitPartition.GetTokenBucketLimiter(
                        key,
                        _ => new TokenBucketRateLimiterOptions
                        {
                            TokenLimit = Math.Max(1, settings.MutationsPerMinute), // burst size
                            ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                            TokensPerPeriod = Math.Max(1, settings.MutationsPerMinute),
                            AutoReplenishment = true,
                            QueueLimit = 0,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                        });
                });

                opts.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(http =>
                {
                    var settings = http.RequestServices.GetRequiredService<IOptions<RateLimitingSettings>>().Value;
                    if (settings.GlobalPerMinute <= 0)
                        return RateLimitPartition.GetNoLimiter("nolimit"); // disable -> no limit

                    string key = GetPartitionKey(http);
                    return RateLimitPartition.GetFixedWindowLimiter(
                        key,
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = settings.GlobalPerMinute,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                        });
                });
            });

            return services;
        }

        /// <summary>
        /// Adds the rate limiting middleware to the application pipeline.
        /// </summary>
        /// <param name="app">The IApplicationBuilder to add the middleware to.</param>
        /// <returns>A configured IApplicationBuilder.</returns>
        public static IApplicationBuilder UseAppRateLimiting(this IApplicationBuilder app) => app.UseRateLimiter();

        /// <summary>
        /// Gets the partition key for rate limiting based on the HTTP context.
        /// </summary>
        /// <param name="http">The current HTTP context.</param>
        /// <returns>A string representing the partition key.</returns>
        private static string GetPartitionKey(HttpContext http)
        {
            if (http.User.Identity?.IsAuthenticated == true)
                return http.User.FindFirst("sub")?.Value ?? "auth_unknown";
            return http.Connection.RemoteIpAddress?.ToString() ?? "ip-unknown";
        }
    }
}