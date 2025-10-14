using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Mail;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
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
            return await db.Users.AnyAsync(u => u.Email == email);
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
        /// Generate a JWT token for a user with the given <paramref name="id"/>
        /// </summary>
        /// <param name="cfg">The configuration containing JWT settings</param>
        /// <param name="id">The user ID</param>
        /// <returns>A JWT token as a <c>string</c></returns>
        /// <exception cref="ArgumentNullException">Thrown if JWT configuration is missing or incomplete</exception>
        public static string GenerateJwtToken(IConfiguration cfg, int id)
        {
            var jwtKey = cfg["Jwt:Key"];
            var jwtIssuer = cfg["Jwt:Issuer"];
            var jwtAudience = cfg["Jwt:Audience"];

            if (string.IsNullOrEmpty(jwtKey) || string.IsNullOrEmpty(jwtIssuer) || string.IsNullOrEmpty(jwtAudience))
                throw new ArgumentNullException("JWT configuration is missing or incomplete.");

            Claim[] claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(jwtKey));
            SigningCredentials creds = new(key, SecurityAlgorithms.HmacSha256);

            JwtSecurityToken token = new(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: DateTime.Now.AddHours(Constants.JwtTokenValidityHours),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
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
    }
}