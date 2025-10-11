using Microsoft.EntityFrameworkCore;
using TuneMates_Backend.DataBase;

namespace TuneMates_Backend.BackgroundServices
{
    public class ProposalCleanupService : RecurringBackgroundService
    {
        public ProposalCleanupService(
            IServiceScopeFactory scopeFactory,
            ILogger<ProposalCleanupService> logger,
            IConfiguration config) : base(scopeFactory, logger, TimeSpan.FromHours(config.GetValue<double>("CleanupService:ProposalIntervalHours", 3)))
        { }

        protected override async Task ExecuteJobAsync(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            DateTime cutoff = DateTime.UtcNow.AddHours(-5); // Proposals older than 5 hours

            int deleted = await db.Songs
                .Where(s => s.AddedAt < cutoff)
                .ExecuteDeleteAsync(stoppingToken);

            _logger.LogInformation("Cleaned up {Count} old proposals at {Time}", deleted, DateTime.UtcNow);
        }
    }
}