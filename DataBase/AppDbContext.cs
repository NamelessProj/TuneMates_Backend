using Microsoft.EntityFrameworkCore;

namespace TuneMates_Backend.DataBase
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();

        public DbSet<Room> Rooms => Set<Room>();

        public DbSet<Song> Songs => Set<Song>();

        public DbSet<Token> Tokens => Set<Token>();

        public DbSet<SpotifyState> SpotifyStates => Set<SpotifyState>();
    }
}