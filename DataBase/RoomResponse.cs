namespace TuneMates_Backend.DataBase
{
    public class RoomResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public RoomResponse() { }

        public RoomResponse(Room r)
        {
            Id = r.Id;
            Name = r.Name;
            Slug = r.Slug;
            IsActive = r.IsActive;
            CreatedAt = r.CreatedAt;
        }
    }
}