namespace TuneMates_Backend.DataBase
{
    public class Room
    {
        public int Id { get; set; }
        /// <value>The ID of the user who created the room</value>
        public int UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public string Market { get; set; } = "CH";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string SpotifyPlaylistId { get; set; } = string.Empty;
    }
}