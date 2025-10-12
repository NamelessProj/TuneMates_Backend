using Microsoft.EntityFrameworkCore;
using TuneMates_Backend.DataBase;

namespace TuneMates_Backend.BackgroundServices
{
    /// <summary>
    /// A background service that periodically cleans up old tokens from the database.
    /// </summary>
    public class TokenCleanupService : RecurringBackgroundService
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TokenCleanupService"/> class.
        /// </summary>
        /// <param name="scopeFactory">The service scope factory for creating scopes.</param>
        /// <param name="logger">The logger instance.</param>
        /// <param name="config">The configuration instance.</param>
        /// <seealso cref="RecurringBackgroundService"/>
        public TokenCleanupService(
            IServiceScopeFactory scopeFactory,
            ILogger<TokenCleanupService> logger,
            IConfiguration config) : base(scopeFactory, logger, TimeSpan.FromHours(config.GetValue<double>("CleanupService:TokenIntervalHours", 3)))
        { }

        /// <summary>
        /// Executes the job to clean up old tokens from the database.
        /// </summary>
        /// <param name="stoppingToken">The cancellation token to stop the job.</param>
        /// <returns>A task that represents the job execution.</returns>
        protected override async Task ExecuteJobAsync(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            DateTime cutoff = DateTime.UtcNow.AddHours(-6); // Tokens older than 6 hours

            int oldTokens = await db.Tokens
                .Where(t => t.CreatedAt < cutoff)
                .ExecuteDeleteAsync(stoppingToken);

            _logger.LogInformation("Cleaned up {Count} old tokens at {Time}", oldTokens, DateTime.UtcNow);
        }
    }
}