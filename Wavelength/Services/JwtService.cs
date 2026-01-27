using Commons.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Wavelength.Data;

namespace Wavelength.Services
{
    public class JwtService(AppDbContext context, IConfiguration configuration)
    {
        public string GenerateJwtToken(User user)
        {
            var Jwt = configuration.GetSection("Jwt");
            var Issuer = Jwt["Issuer"];
            var Audience = Jwt["Audience"];
            var Secret = Jwt["Secret"];
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var Claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim("FirstName", user.FirstName),
                new Claim("LastName", user.LastName),
                new Claim("Birthday", user.Birthday.ToString()),
                new Claim("Email", user.Email)
            };

            foreach (var userRole in user.UserRoles)
            {
                Claims.Add(new Claim(ClaimTypes.Role, userRole.Role.ToString()));
            }

            var token = new JwtSecurityToken(
                issuer: Issuer,
                audience: Audience,
                claims: Claims,
                expires: DateTime.UtcNow.AddMinutes(30),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<string> GenerateRefreshToken()
        {
            while (true)
            {
                var randomNumber = new byte[32];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(randomNumber);
                    var token = Convert.ToBase64String(randomNumber);
                    if (await context.RefreshTokens.FindAsync(token) == null) return token;
                }
            }
        }
    }
}
