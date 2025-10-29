using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace TuneMates_Backend.Infrastructure.Auth
{
    public class JwtTokenService
    {
        private readonly JwtSettings _settings;

        /// <summary>
        /// Initializes a new instance of the <see cref="JwtTokenService"/> class.
        /// </summary>
        /// <param name="options">The JWT settings options.</param>
        public JwtTokenService(IOptions<JwtSettings> options) => _settings = options.Value;

        /// <summary>
        /// Generates a JWT token for the specified user ID with optional extra claims and lifetime.
        /// </summary>
        /// <param name="userId">The user ID to include in the token.</param>
        /// <param name="extraClaims">The extra claims to include in the token.</param>
        /// <param name="lifetime">The lifetime of the token. If null, the default from settings is used.</param>
        /// <returns>A JWT token as a string.</returns>
        public string GenerateToken(int userId, IEnumerable<Claim>? extraClaims = null, TimeSpan? lifetime = null)
        {
            List<Claim> claims = new()
            {
                new(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };
            if (extraClaims != null)
                claims.AddRange(extraClaims);

            SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(_settings.Key));
            SigningCredentials creds = new(key, SecurityAlgorithms.HmacSha256);

            JwtSecurityToken token = new(
                issuer: _settings.Issuer,
                audience: _settings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.Add(lifetime ?? TimeSpan.FromMinutes(_settings.ExpiresInMinutes)),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}