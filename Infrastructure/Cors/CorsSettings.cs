namespace TuneMates_Backend.Infrastructure.Cors
{
    public class CorsSettings
    {
        public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
        public bool AllowCredentials { get; set; } = false;
    }
}