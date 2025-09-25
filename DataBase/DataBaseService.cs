using Microsoft.EntityFrameworkCore;

namespace TuneMates_Backend.DataBase
{
    public class DataBaseService
    {
        public static void EnsureDatabaseCreated(IServiceProvider sp)
        {
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.Migrate();
        }
    }
}