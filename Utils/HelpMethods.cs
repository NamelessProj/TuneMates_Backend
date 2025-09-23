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

        public static string GenerateSlug(string s)
        {
            // Convert to lower case
            s = s.ToLowerInvariant();
            // Replace spaces with hyphens
            s = System.Text.RegularExpressions.Regex.Replace(s, @"\s+", "-");
            // Remove invalid characters
            s = System.Text.RegularExpressions.Regex.Replace(s, @"[^a-z0-9\-]", "");
            // Remove multiple hyphens
            s = System.Text.RegularExpressions.Regex.Replace(s, @"-+", "-");
            // Trim hyphens from start and end
            s = s.Trim('-');
            return s;
        }

        public static bool IsPasswordValid(string password)
        {
            // At least 8 characters, at least one uppercase letter, one lowercase letter, one digit, and one special character
            if (password.Length < 8)
                return false;
            if (!System.Text.RegularExpressions.Regex.IsMatch(password, @"[A-Z]"))
                return false;
            if (!System.Text.RegularExpressions.Regex.IsMatch(password, @"[a-z]"))
                return false;
            if (!System.Text.RegularExpressions.Regex.IsMatch(password, @"[0-9]"))
                return false;
            if (!System.Text.RegularExpressions.Regex.IsMatch(password, @"[\W_]"))
                return false;
            return true;
        }
    }
}