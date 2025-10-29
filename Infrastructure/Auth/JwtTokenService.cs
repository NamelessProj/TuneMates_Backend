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

        public JwtTokenService(IOptions<JwtSettings> options) => _settings = options.Value;

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