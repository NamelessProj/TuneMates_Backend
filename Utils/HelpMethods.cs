using Microsoft.EntityFrameworkCore;
using System.Net.Mail;
using TuneMates_Backend.DataBase;

namespace TuneMates_Backend.Utils
{
    public static class HelpMethods
    {
        public static async Task<bool> IsEmailInUse(AppDbContext db, string email)
        {
            return await db.Users.AnyAsync(u => u.Email == email);
        }

        public static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}