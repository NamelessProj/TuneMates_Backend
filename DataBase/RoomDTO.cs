namespace TuneMates_Backend.DataBase
{
    public class RoomDTO
    {
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public string Password { get; set; } = string.Empty;
        public string PasswordConfirm { get; set; } = string.Empty;
        public string SpotifyPlaylistId { get; set; } = string.Empty;
    }
}