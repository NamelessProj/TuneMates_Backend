using Microsoft.EntityFrameworkCore;
using TuneMates_Backend.DataBase;
using TuneMates_Backend.Utils;

namespace TuneMates_Backend.BackgroundServices
{
    /// <summary>
    /// A background service that periodically cleans up old song proposals from the database.
    /// <seealso cref="RecurringBackgroundService"/>
    /// </summary>
    public class ProposalCleanupService : RecurringBackgroundService
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProposalCleanupService"/> class.
        /// </summary>
        /// <param name="scopeFactory">The service scope factory for creating scopes.</param>
        /// <param name="logger">The logger instance.</param>
        /// <param name="config">The configuration instance.</param>
        public ProposalCleanupService(
            IServiceScopeFactory scopeFactory,
            ILogger<ProposalCleanupService> logger,
            IConfiguration config) : base(scopeFactory, logger, TimeSpan.FromHours(config.GetValue<double>("CleanupService:ProposalIntervalHours", Constants.DefaultBackgroundServiceIntervalHours)))
        { }

        /// <summary>
        /// Executes the job to clean up old song proposals from the database.
        /// </summary>
        /// <param name="stoppingToken">The cancellation token to stop the job.</param>
        /// <returns>A task that represents the job execution.</returns>
        protected override async Task ExecuteJobAsync(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            DateTime cutoff = DateTime.UtcNow.AddHours(-Constants.MaxHoursForAProposalBeforeCleanup); // Proposals older than 5 hours

            int deleted = await db.Songs
                .Where(s => s.AddedAt < cutoff)
                .ExecuteDeleteAsync(stoppingToken);

            _logger.LogInformation("Cleaned up {Count} old proposals at {Time}", deleted, DateTime.UtcNow);
        }
    }
}