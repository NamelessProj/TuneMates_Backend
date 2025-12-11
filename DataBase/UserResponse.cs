namespace TuneMates_Backend.DataBase
{
    public class UserResponse
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool SpotifyId { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public UserResponse() { }

        /// <summary>
        /// Constructs a UserResponse object from a <see cref="User"/> object.
        /// </summary>
        /// <param name="user">The <see cref="User"/> object to convert.</param>
        public UserResponse(User user)
        {
            Id = user.Id;
            Username = user.Username;
            Email = user.Email;
            CreatedAt = user.CreatedAt;
            SpotifyId = user.SpotifyId != string.Empty;
        }
    }
}