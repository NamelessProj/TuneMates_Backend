namespace TuneMates_Backend.DataBase
{
    public class Token
    {
        public int Id { get; set; }
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddHours(1);
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}