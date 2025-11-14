using Microsoft.EntityFrameworkCore;
using TuneMates_Backend.DataBase;
using TuneMates_Backend.Utils;

namespace TuneMates_Backend.BackgroundServices
{
    /// <summary>
    /// A background service that periodically cleans up old SpotifyState entries from the database.
    /// </summary>
    public class SpotifyStateCleanupService : RecurringBackgroundService
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SpotifyStateCleanupService"/> class.
        /// </summary>
        /// <param name="scopeFactory">The service scope factory for creating scopes.</param>
        /// <param name="logger">The logger instance.</param>
        /// <param name="config">The configuration instance.</param>
        public SpotifyStateCleanupService(
            IServiceScopeFactory scopeFactory,
            ILogger<SpotifyStateCleanupService> logger,
            IConfiguration config) : base(scopeFactory, logger, TimeSpan.FromHours(config.GetValue<double>("CleanupService:SpotifyStateIntervalHours", Constants.Cleanup.DefaultBackgroundServiceIntervalHours)))
        { }

        /// <summary>
        /// Executes the cleanup job to delete old SpotifyState entries.
        /// </summary>
        /// <param name="stoppingToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected override async Task ExecuteJobAsync(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            DateTime cutoff = DateTime.UtcNow.AddHours(-Constants.Cleanup.MaxHoursForSpotifyStateBeforeCleanup); // Define cutoff time for deletion all SpotifyStates older than this
            int deletedCount = await db.SpotifyStates
                .Where(s => s.CreatedAt < cutoff)
                .ExecuteDeleteAsync(stoppingToken);

            _logger.LogInformation("{Service}: Deleted {DeletedCount} SpotifyState entries older than {Cutoff}", GetType().Name, deletedCount, cutoff);
        }
    }
}