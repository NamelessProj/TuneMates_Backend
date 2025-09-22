using Microsoft.EntityFrameworkCore;
using TuneMates_Backend.DataBase;

namespace TuneMates_Backend.Utils
{
    public static class HelpMethods
    {
        public static async Task<bool> IsEmailInUse(AppDbContext db, string email)
        {
            return await db.Users.AnyAsync(u => u.Email == email);
        }
    }
}