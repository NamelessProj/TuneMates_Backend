namespace TuneMates_Backend.DataBase
{
    public class Song
    {
        public int Id { get; set; }
        public int RoomId { get; set; }
        /// <value>The Spotify ID of the song</value>
        public string SongId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Artist { get; set; } = string.Empty;
        public string Album { get; set; } = string.Empty;
        public string AlbumArtUrl { get; set; } = string.Empty;
        public int DurationMs { get; set; }
        public SongStatus Status { get; set; } = SongStatus.Pending;
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    }
}