using Microsoft.EntityFrameworkCore;
using TuneMates_Backend.DataBase;
using TuneMates_Backend.Utils;

namespace TuneMates_Backend.BackgroundServices
{
    /// <summary>
    /// A background service that periodically cleans up expired room codes from the database.
    /// <seealso cref="RecurringBackgroundService"/>
    /// </summary>
    public class RoomCodeCleanupService : RecurringBackgroundService
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RoomCodeCleanupService"/> class.
        /// </summary>
        /// <param name="scopeFactory">The service scope factory for creating scopes.</param>
        /// <param name="logger">The logger instance.</param>
        /// <param name="config">The configuration instance.</param>
        /// /// <seealso cref="RecurringBackgroundService"/>
        public RoomCodeCleanupService(
            IServiceScopeFactory scopeFactory,
            ILogger<RoomCodeCleanupService> logger,
            IConfiguration config) : base(scopeFactory, logger, TimeSpan.FromHours(config.GetValue<double>("CleanupService:RoomCodeIntervalHours", Constants.Cleanup.DefaultBackgroundServiceIntervalHours)))
        { }

        /// <summary>
        /// Executes the job of cleaning up expired room codes from the database.
        /// </summary>
        /// <param name="stoppingToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected override async Task ExecuteJobAsync(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            int deletedCount = await db.RoomCodes
                .Where(rc => rc.ExpiresAt <= DateTime.UtcNow)
                .ExecuteDeleteAsync(stoppingToken);

            _logger.LogInformation("{Service}: Cleaned up {Count} codes at {Time}", GetType().Name, deletedCount, DateTime.UtcNow);
        }
    }
}