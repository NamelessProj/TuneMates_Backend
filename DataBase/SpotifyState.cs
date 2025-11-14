namespace TuneMates_Backend.DataBase
{
    public class SpotifyState
    {
        public int Id { get; set; }
        public string State { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}