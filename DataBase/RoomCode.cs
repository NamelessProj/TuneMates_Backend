using TuneMates_Backend.Utils;

namespace TuneMates_Backend.DataBase
{
    public class RoomCode
    {
        public string Code { get; set; } = string.Empty;
        public int RoomId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddHours(Constants.MinHoursForACodeBeforeExpiry);
    }
}