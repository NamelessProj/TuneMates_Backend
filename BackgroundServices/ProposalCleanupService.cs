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
            IConfiguration config) : base(scopeFactory, logger, TimeSpan.FromHours(config.GetValue<double>("CleanupService:ProposalIntervalHours", Constants.Cleanup.DefaultBackgroundServiceIntervalHours)))
        { }

        /// <summary>
        /// Executes the job to clean up old song proposals from the database.
        /// </summary>
        /// <param name="stoppingToken">The cancellation token to stop the job.</param>
        /// <returns>A task that represents the job execution.</returns>
        protected override async Task ExecuteJobAsync(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            DateTime refusedCutoff = DateTime.UtcNow.AddHours(-Constants.Cleanup.MaxHoursForProposalBeforeRefused);
            int refused = await db.Songs
                .Where(s => s.AddedAt < refusedCutoff)
                .ExecuteUpdateAsync(s => s.SetProperty(song => song.Status, SongStatus.Refused), stoppingToken);

            _logger.LogInformation("{Service}: set {Count} songs as refused at {TIme}", GetType().Name, refused, DateTime.UtcNow);

            DateTime deletedCutoff = DateTime.UtcNow.AddHours(-Constants.Cleanup.MaxHoursForAProposalBeforeCleanup); // Proposals older than 5 hours
            int deleted = await db.Songs
                .Where(s => s.AddedAt < deletedCutoff)
                .ExecuteDeleteAsync(stoppingToken);

            _logger.LogInformation("{Service}: Cleaned up {Count} old proposals at {Time}", GetType().Name, deleted, DateTime.UtcNow);
        }
    }
}