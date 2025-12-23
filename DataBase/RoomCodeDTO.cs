namespace TuneMates_Backend.DataBase
{
    public class RoomCodeDTO
    {
        public string Password { get; set; } = string.Empty;
        public int ExpiresInHours { get; set; } = 1;
    }
}