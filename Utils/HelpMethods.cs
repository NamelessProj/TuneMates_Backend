using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Mail;
using System.Security.Claims;
using System.Text;
using TuneMates_Backend.DataBase;

namespace TuneMates_Backend.Utils
{
    public static class HelpMethods
    {
        public static async Task<bool> IsEmailInUse(AppDbContext db, string email)
        {
            return await db.Users.AnyAsync(u => u.Email == email);
        }

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
                expires: DateTime.Now.AddHours(3),
                signingCredentials: creds
                );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

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