using Microsoft.EntityFrameworkCore;
using TuneMates_Backend.DataBase;

namespace TuneMates_Backend.BackgroundServices
{
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

        protected override async Task ExecuteJobAsync(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            DateTime cutoff = DateTime.UtcNow.AddHours(-6); // Tokens older than 6 hours
            var oldTokens = await db.Tokens
                .Where(t => t.CreatedAt < cutoff)
                .ToListAsync(stoppingToken);

            if (oldTokens.Count == 0)
            {
                _logger.LogInformation("No old tokens to clean up at {Time}", DateTime.UtcNow);
                return;
            }

            await db.Tokens
                .Where(t => t.CreatedAt < cutoff)
                .ExecuteDeleteAsync(stoppingToken);

            _logger.LogInformation("Cleaned up {Count} old tokens at {Time}", oldTokens.Count, DateTime.UtcNow);
        }
    }
}