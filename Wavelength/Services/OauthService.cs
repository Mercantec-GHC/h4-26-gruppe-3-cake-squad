using Commons.Models.Database;
using Commons.Models.Dtos;
using Commons.Models.Oauth;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Text.Json;
using Wavelength.Data;

namespace Wavelength.Services
{
    /// <summary>
    /// Provides OAuth-related authentication services, including handling Google OAuth callbacks and retrieving user
    /// profile information from Google.
    /// </summary>
    /// <remarks>The OauthService enables integration with Google OAuth for user authentication and profile
    /// retrieval. It manages user creation and authentication responses based on Google account information. The
    /// service requires valid configuration settings and access to the application's database context. All operations
    /// are performed asynchronously.</remarks>
    public class OauthService
    {
        private readonly AppDbContext dbContext;
        private readonly JwtService jwtService;
        private readonly IConfiguration configuration;

        /// <summary>
        /// Initializes a new instance of the OauthService class with the specified database context, JWT service, and
        /// configuration settings.
        /// </summary>
        /// <param name="dbContext">The database context used for accessing and managing application data related to authentication.</param>
        /// <param name="jwtService">The service responsible for generating and validating JSON Web Tokens (JWT) for authentication operations.</param>
        /// <param name="configuration">The configuration settings used to retrieve application-specific options and secrets required for OAuth
        /// operations.</param>
        public OauthService(AppDbContext dbContext, JwtService jwtService, IConfiguration configuration)
        {
            this.dbContext = dbContext;
            this.jwtService = jwtService;
            this.configuration = configuration;
        }

        /// <summary>
        /// Handles the Google OAuth callback by exchanging the authorization code for tokens, retrieving user
        /// information from Google, and returning an authentication response for the user.
        /// </summary>
        /// <remarks>This method creates a new user in the database if the Google account does not already
        /// exist. The user's email is considered verified based on Google's authentication. The method requires valid
        /// Google OAuth configuration settings and access to the database context.</remarks>
        /// <param name="code">The authorization code received from Google's OAuth flow. Must be a valid, non-empty code provided by Google
        /// after user consent.</param>
        /// <returns>An authentication response containing user information and tokens. The response represents the authenticated
        /// user associated with the provided Google account.</returns>
        /// <exception cref="Exception">Thrown if the ID token cannot be retrieved from Google, indicating an unsuccessful token exchange or invalid
        /// authorization code.</exception>
        public async Task<AuthResponseDto> HandleGoogleCallback(string code)
        {
            var client = new HttpClient();

            var dict = new Dictionary<string, string>
            {
                { "code", code },
                { "client_id", configuration["Oauth:Google:ClientId"]! },
                { "client_secret", configuration["Oauth:Google:ClientSecret"]! },
                { "redirect_uri", configuration["Oauth:Google:RedirectUri"]! },
                { "grant_type", "authorization_code" }
            };

            // Exchange code for tokens
            var response = await client.PostAsync(
                "https://oauth2.googleapis.com/token",
                new FormUrlEncodedContent(dict));

            // Ensure we got a successful response
            var json = await response.Content.ReadAsStringAsync();
            var token = JsonSerializer.Deserialize<GoogleTokenResponse>(json);
            if (token == null || string.IsNullOrEmpty(token.id_token))
                throw new Exception("Failed to retrieve ID token from Google.");

            // Decode ID token (fastest way)
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token.id_token);

            var googleUser = new
            {
                Email = jwt.Claims.First(c => c.Type == "email").Value,
                FirstName = jwt.Claims.First(c => c.Type == "given_name").Value,
                LastName = jwt.Claims.First(c => c.Type == "family_name").Value,
                Picture = jwt.Claims.First(c => c.Type == "picture").Value,
                GoogleId = jwt.Claims.First(c => c.Type == "sub").Value
            };

            // Get birthday from People API
            //var birthday = await GetGoogleBirthdayAsync(token.access_token);
            var birthday = await GetGoogleBirthdayAsync(token.access_token);

            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == googleUser.Email);
            if (user == null)
            {
                user = new User
                {
                    Email = googleUser.Email,
                    FirstName = googleUser.FirstName,
                    LastName = googleUser.LastName,
                    Birthday = birthday,
                    HashedPassword = string.Empty, // No password since it's OAuth
                    IsEmailVerified = true // Assume verified since it's from Google
                };
                dbContext.Users.Add(user);
                await dbContext.SaveChangesAsync();
            }

            return await jwtService.CreateAuthResponseAsync(user);
        }

        /// <summary>
        /// Retrieves the user's birthday from their Google profile using the People API.
        /// </summary>
        /// <remarks>This method requires the access token to have appropriate scopes (such as 'profile'
        /// or 'https://www.googleapis.com/auth/user.birthday.read') to access birthday information. The returned date
        /// may not include the year if the user has chosen to keep it private. If the birthday is unavailable or an
        /// error occurs, the method returns <see cref="DateOnly.MinValue"/> (year 1, month 1, day 1).</remarks>
        /// <param name="accessToken">The OAuth 2.0 access token used to authorize the request to the Google People API. Must be valid and
        /// authorized to access the user's profile information.</param>
        /// <returns>A <see cref="DateOnly"/> representing the user's birthday. If the year is not available, the year component
        /// is set to 1. If the birthday cannot be retrieved, returns <see cref="DateOnly.MinValue"/>.</returns>
        public async Task<DateOnly> GetGoogleBirthdayAsync(string accessToken)
        {
            try
            {
                var url = "https://people.googleapis.com/v1/people/me?personFields=birthdays";

                using var client = new HttpClient();
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", accessToken);

                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();

                var doc = JsonDocument.Parse(json);

                foreach (var b in doc.RootElement.GetProperty("birthdays").EnumerateArray())
                {
                    var date = b.GetProperty("date");

                    // Attempt to get birthday with year
                    if (date.TryGetProperty("year", out var y))
                    {
                        return new DateOnly(
                            y.GetInt32(),
                            date.GetProperty("month").GetInt32(),
                            date.GetProperty("day").GetInt32()
                        );
                    }
                }

                // Fall back to the primary without year
                var primary = doc.RootElement.GetProperty("birthdays")[0].GetProperty("date");
                return new DateOnly(
                    1,
                    primary.GetProperty("month").GetInt32(),
                    primary.GetProperty("day").GetInt32()
                );
            }
            catch
            {
                // If anything goes wrong, just return a default date (could also choose to throw or return null)
                return new DateOnly(1, 1, 1);
            }
        }
    }
}
