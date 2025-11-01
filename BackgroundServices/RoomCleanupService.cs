using Microsoft.EntityFrameworkCore;
using TuneMates_Backend.DataBase;
using TuneMates_Backend.Utils;

namespace TuneMates_Backend.BackgroundServices
{
    /// <summary>
    /// A background service that periodically sets inactive rooms and cleans up old inactive rooms from the database.
    /// </summary>
    public class RoomCleanupService : RecurringBackgroundService
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RoomCleanupService"/> class.
        /// </summary>
        /// <param name="scopeFactory">The service scope factory for creating scopes.</param>
        /// <param name="logger">The logger instance.</param>
        /// <param name="config">The configuration instance.</param>
        public RoomCleanupService(
            IServiceScopeFactory scopeFactory,
            ILogger<ProposalCleanupService> logger,
            IConfiguration config) : base(scopeFactory, logger, TimeSpan.FromHours(config.GetValue<double>("CleanupService:RoomIntervalHours", Constants.Cleanup.DefaultBackgroundServiceIntervalHours)))
        { }

        /// <summary>
        /// Executes the job to set inactive rooms and clean up old inactive rooms from the database.
        /// </summary>
        /// <param name="stoppingToken">The cancellation token to stop the job.</param>
        /// <returns>A task that represents the job execution.</returns>
        protected override async Task ExecuteJobAsync(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Change status of rooms inactive for more than the defined hours to inactive
            DateTime cutoff = DateTime.UtcNow.AddHours(-Constants.Cleanup.MaxHoursForARoomBeforeInactive); // Rooms inactive for more than defined hours
            int updated = await db.Rooms
                .Where(r => r.IsActive && r.LastUpdate < cutoff)
                .ExecuteUpdateAsync(r => r.SetProperty(room => room.IsActive, false), stoppingToken);

            _logger.LogInformation("Marked {Count} rooms as inactive at {Time}", updated, DateTime.UtcNow);

            // Delete rooms that have been inactive for more than the defined days
            DateTime deleteCutoff = DateTime.UtcNow.AddDays(-Constants.Cleanup.MaxDaysForARoomBeforeCleanup); // Rooms inactive for more than defined days
            int deleted = await db.Rooms
                .Where(r => !r.IsActive && r.LastUpdate < deleteCutoff)
                .ExecuteDeleteAsync(stoppingToken);

            _logger.LogInformation("Cleaned up {Count} old inactive (inactive for >= {Days} days) rooms at {Time}", deleted, Constants.MaxDaysForARoomBeforeCleanup, DateTime.UtcNow);
        }
    }
}