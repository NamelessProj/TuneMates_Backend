using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Mail;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TuneMates_Backend.DataBase;

namespace TuneMates_Backend.Utils
{
    public static class HelpMethods
    {
        /// <summary>
        /// Check if an email is already in use in the database
        /// </summary>
        /// <param name="db">The database context</param>
        /// <param name="email">The email to check</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous operation. The task result contains true if the email is in use, otherwise false.</returns>
        public static async Task<bool> IsEmailInUseAsync(AppDbContext db, string email)
        {
            string normalizedEmail = email.Trim().ToLowerInvariant();
            return await db.Users.AnyAsync(u => u.Email == normalizedEmail);
        }

        /// <summary>
        /// Validate the format of an email address
        /// </summary>
        /// <param name="email">The email address to validate</param>
        /// <returns>A <c>bool</c> indicating whether the email format is valid</returns>
        public static bool IsEmailValid(string email)
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

        /// <summary>
        /// Validate password complexity
        /// </summary>
        /// <param name="password">The password to validate</param>
        /// <returns>A <c>bool</c> indicating whether the password meets complexity requirements</returns>
        public static bool IsPasswordValid(string password)
        {
            // At least 8 characters, at least one uppercase letter, one lowercase letter, one digit, and one special character
            if (password.Length < 8)
                return false;
            if (!Regex.IsMatch(password, @"[A-Z]"))
                return false;
            if (!Regex.IsMatch(password, @"[a-z]"))
                return false;
            if (!Regex.IsMatch(password, @"[0-9]"))
                return false;
            if (!Regex.IsMatch(password, @"[\W_]"))
                return false;
            return true;
        }

        /// <summary>
        /// Generate a URL-friendly slug from a given string
        /// </summary>
        /// <param name="s">The input string</param>
        /// <returns>A URL-friendly slug</returns>
        public static string GenerateSlug(string s)
        {
            // Convert to lower case
            s = s.ToLowerInvariant();
            // Replace spaces with hyphens
            s = Regex.Replace(s, @"\s+", "-");
            // Remove invalid characters
            s = Regex.Replace(s, @"[^a-z0-9\-]", "");
            // Remove multiple hyphens
            s = Regex.Replace(s, @"-+", "-");
            // Trim hyphens from start and end
            s = s.Trim('-');
            return s;
        }

        /// <summary>
        /// Generate a random hexadecimal string of the specified <paramref name="length"/>
        /// </summary>
        /// <param name="length">The desired length of the random string</param>
        /// <returns>A random hexadecimal string</returns>
        public static string GenerateRandomString(int length)
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                // Generate random bytes (60 bytes to ensure enough length after hex conversion)
                byte[] randomBytes = new byte[60];
                rng.GetBytes(randomBytes);

                // Convert bytes to hexadecimal string
                StringBuilder hex = new(randomBytes.Length * 2);
                foreach (byte b in randomBytes)
                    hex.AppendFormat("{0:x2}", b);

                // Return the string truncated to the desired length
                return hex.ToString().Substring(0, Math.Min(length, hex.Length));
            }
        }

        /// <summary>
        /// Extract the user ID from JWT claims in the given <paramref name="http"/> context
        /// </summary>
        /// <param name="http">The current HTTP context</param>
        /// <returns>The user ID as a nullable <c>int</c>, or <c>null</c> if not found or invalid</returns>
        public static int? GetUserIdFromJwtClaims(HttpContext http)
        {
            var claims = http.User.Claims.Select(c => new { c.Type, c.Value }).ToList();

            var userIdClaim = http.User.Claims.FirstOrDefault(c =>
                c.Type == JwtRegisteredClaimNames.Sub ||
                c.Type == ClaimTypes.NameIdentifier ||
                c.Type.EndsWith("/nameidentifier")
            );
            var userId = userIdClaim != null && int.TryParse(userIdClaim.Value, out var id) ? id : 0;

            return userId == 0 ? null : userId;
        }

        /// <summary>
        /// Check if a room slug is already in use, and modify it if necessary
        /// </summary>
        /// <param name="slug">The original slug</param>
        /// <param name="db">The database context</param>
        /// <param name="userId">The user ID to append if the slug is taken</param>
        /// <returns>A <see cref="Task"/> that represents the asynchronous operation. The task result contains the available slug, or an empty string if no unique slug could be generated.</returns>
        public static async Task<string?> IsSlugAlreadyInUse(this string slug, AppDbContext db, int userId)
        {
            async Task<bool> checkSlug(string s) => await db.Rooms.AnyAsync(r => r.Slug == s);

            if (!await checkSlug(slug))
                return slug;
            
            slug += $"-{userId}";
            return await checkSlug(slug) ? slug : null;
        }
    }
}