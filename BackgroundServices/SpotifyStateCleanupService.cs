using Microsoft.EntityFrameworkCore;
using TuneMates_Backend.DataBase;
using TuneMates_Backend.Utils;

namespace TuneMates_Backend.BackgroundServices
{
    public class SpotifyStateCleanupService : RecurringBackgroundService
    {
        public SpotifyStateCleanupService(
            IServiceScopeFactory scopeFactory,
            ILogger<SpotifyStateCleanupService> logger,
            IConfiguration config) : base(scopeFactory, logger, TimeSpan.FromHours(config.GetValue<double>("CleanupService:SpotifyStateIntervalHours", Constants.Cleanup.DefaultBackgroundServiceIntervalHours)))
        { }

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