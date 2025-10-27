namespace TuneMates_Backend.Infrastructure.RateLimiting
{
    public class RateLimitingSettings
    {
        public int GlobalPerMinute { get; set; } = 0; // 0 means disabled
        public int SearchTightPerMinute { get; set; } = 30;
        public int MutationsPerMinute { get; set; } = 10;
    }
}