using Microsoft.EntityFrameworkCore;
using TuneMates_Backend.DataBase;
using TuneMates_Backend.Utils;

namespace TuneMates_Backend.BackgroundServices
{
    /// <summary>
    /// A background service that periodically cleans up old tokens from the database.
    /// <seealso cref="RecurringBackgroundService"/>
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
            IConfiguration config) : base(scopeFactory, logger, TimeSpan.FromHours(config.GetValue<double>("CleanupService:TokenIntervalHours", Constants.Cleanup.DefaultBackgroundServiceIntervalHours)))
        { }

        /// <summary>
        /// Executes the job to clean up old tokens from the database.
        /// </summary>
        /// <param name="stoppingToken">The cancellation token to stop the job.</param>
        /// <returns>A task that represents the job execution.</returns>
        protected override async Task ExecuteJobAsync(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            int deltedTokens = await db.Tokens
                .Where(t => t.ExpiresAt < DateTime.UtcNow)
                .ExecuteDeleteAsync(stoppingToken);

            _logger.LogInformation("{Service}: Cleaned up {Count} tokens at {Time}", GetType().Name, deltedTokens, DateTime.UtcNow);
        }
    }
}