namespace TuneMates_Backend.DataBase
{
    public class RoomResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public string Market { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public RoomResponse() { }

        /// <summary>
        /// Constructs a RoomResponse object from a <see cref="Room"/> object.
        /// </summary>
        /// <param name="r">The <see cref="Room"/> object to convert.</param>
        public RoomResponse(Room r)
        {
            Id = r.Id;
            Name = r.Name;
            Slug = r.Slug;
            IsActive = r.IsActive;
            Market = r.Market;
            CreatedAt = r.CreatedAt;
        }
    }
}