using Commons.Models.DatabaseModels;
using Commons.Models.Dtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Wavelength.Data;

namespace Wavelength.Services
{
    public class JwtService(AppDbContext dbContext, IConfiguration configuration)
    {
        /// <summary>
        /// Generates a JSON Web Token (JWT) that encodes the specified user's identity and roles.
        /// </summary>
        /// <remarks>The generated token includes claims for the user's ID, first name, last name,
        /// birthday, email, and all associated roles. The token is signed using the HMAC SHA-256 algorithm and
        /// configuration values for issuer, audience, and secret key. Ensure that the configuration contains valid JWT
        /// settings before calling this method.</remarks>
        /// <param name="user">The user for whom to generate the JWT. Must not be null and should contain valid identity and role
        /// information.</param>
        /// <returns>A JWT as a string that represents the user's identity and roles. The token is valid for 30 minutes from the
        /// time of generation.</returns>
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

            foreach (var role in user.Roles)
            {
                Claims.Add(new Claim(ClaimTypes.Role, role.ToString()));
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

        /// <summary>
        /// Generates a new, unique refresh token for authentication purposes.
        /// </summary>
        /// <remarks>The generated token is guaranteed to be unique within the current set of refresh
        /// tokens stored in the database. This method uses a cryptographically secure random number generator to ensure
        /// token unpredictability. The operation may take longer if the token space is heavily populated, as it retries
        /// until a unique token is found.</remarks>
        /// <returns>A base64-encoded string representing a cryptographically secure, unique refresh token.</returns>
        public async Task<string> GenerateRefreshTokenAsync()
        {
            while (true)
            {
                var randomNumber = new byte[32];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(randomNumber);
                    var token = Convert.ToBase64String(randomNumber);
                    if (await dbContext.RefreshTokens.FindAsync(token) == null) return token;
                }
            }
        }

        /// <summary>
        /// Validates the specified refresh token and revokes it if valid, returning the associated user.
        /// </summary>
        /// <remarks>This method revokes the refresh token upon successful validation, preventing it from
        /// being used again. If the token is invalid, revoked, expired, or not found, no changes are made and null is
        /// returned.</remarks>
        /// <param name="refreshToken">The refresh token to validate. Cannot be null or empty.</param>
        /// <returns>The user associated with the valid refresh token; otherwise, null if the token is invalid, revoked, expired,
        /// or not found.</returns>
        public async Task<User?> ValidateRefreshTokenAsync(string refreshToken)
        {
            // Check if the refresh token is provided
            if (string.IsNullOrEmpty(refreshToken)) return null;

            // Check if the refresh token exists, is not revoked, and has not expired
            var token = await dbContext.RefreshTokens.Include(rt => rt.User)
                .ThenInclude(u => u.UserRoles)
                .FirstOrDefaultAsync(rt => rt.Id == refreshToken && !rt.IsRevoked && rt.ExpiryDate > DateTime.UtcNow);
            if (token == null) return null;

            // Revoke the used refresh token
            token.IsRevoked = true;
            await dbContext.SaveChangesAsync();

            return token.User;
        }

        /// <summary>
        /// Creates an authentication response containing a JWT access token and a refresh token for the specified user.
        /// </summary>
        /// <remarks>The generated refresh token is persisted for future authentication requests. The JWT
        /// token expires after 1800 seconds (30 minutes), while the refresh token is valid for 7 days.</remarks>
        /// <param name="user">The user for whom the authentication response is generated. Cannot be null.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains an <see cref="AuthResponseDto"/>
        /// with the generated JWT token, refresh token, and token expiry information.</returns>
        public async Task<AuthResponseDto> CreateAuthResponseAsync(User user)
        {
            var jwtToken = GenerateJwtToken(user);
            var refreshTokenString = await GenerateRefreshTokenAsync();
            int expiry = 1800;

            // Store refresh token in database
            var refreshToken = new RefreshToken
            {
                UserId = user.Id,
                ExpiryDate = DateTime.UtcNow.AddDays(7),
                IsRevoked = false,
                Id = refreshTokenString
            };
            await dbContext.RefreshTokens.AddAsync(refreshToken);
            await dbContext.SaveChangesAsync();

            // Return authentication response
            return new AuthResponseDto
            {
                JwtToken = jwtToken,
                RefreshToken = refreshTokenString,
                Expires = expiry
            };
        }
    }
}
