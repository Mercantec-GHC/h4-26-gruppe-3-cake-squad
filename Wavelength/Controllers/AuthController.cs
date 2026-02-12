using Commons.Models.Database;
using Commons.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using Wavelength.Data;
using Wavelength.Services;

namespace Wavelength.Controllers
{
    /// <summary>
    /// Provides API endpoints for user authentication, registration, profile management, and related operations.
    /// </summary>
    /// <remarks>This controller handles user authentication workflows, including registration, login, email
    /// verification, token refresh, password updates, and profile retrieval. All endpoints are routed under the
    /// "[controller]" route and use dependency-injected services for authentication logic. Some actions require the
    /// user to be authenticated via authorization attributes.</remarks>
    [ApiController]
    [Route("[controller]")]
    public class AuthController : BaseController
    {
        private readonly AuthService authService;
		private readonly IConfiguration configuration;
		private readonly JwtService jwtService;

		/// <summary>
		/// Initializes a new instance of the AuthController class with the specified database context and
		/// authentication service.
		/// </summary>
		/// <param name="dbContext">The database context used to access application data.</param>
		/// <param name="authService">The authentication service used to handle user authentication operations.</param>
		public AuthController(AppDbContext dbContext, AuthService authService, IConfiguration configuration, JwtService jwtService) : base(dbContext)
        {
            this.authService = authService;
			this.configuration = configuration;
			this.jwtService = jwtService;
		}

        /// <summary>
        /// Registers a new user account using the specified registration details.
        /// </summary>
        /// <param name="dto">An object containing the user's registration information. Cannot be null.</param>
        /// <returns>A 201 Created result if registration is successful; otherwise, a 400 Bad Request result with an error
        /// message if the registration details are invalid.</returns>
        [HttpPost("register")]
        public async Task<ActionResult> RegisterAsync(RegisterDto dto)
        {
            try
            {
                await authService.RegisterUserAsync(dto);
                return Created();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Verifies a user's email address using the provided validation data.
        /// </summary>
        /// <param name="dto">An object containing the email verification data required to complete the verification process. Cannot be
        /// null.</param>
        /// <returns>An HTTP 200 OK result if the email is verified successfully; otherwise, an HTTP 400 Bad Request result with
        /// an error message if the verification fails.</returns>
        [HttpPost("verifyEmail")]
        public async Task<ActionResult> VerifyEmailAsync(ValidateDto dto)
        {
            try
            {
                await authService.VerifyEmailAsync(dto);
                return Ok("Email verified successfully.");
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Authenticates a user with the provided credentials and returns an authentication token if successful.
        /// </summary>
        /// <param name="dto">The login credentials and related information for the user attempting to authenticate. Cannot be null.</param>
        /// <returns>An <see cref="ActionResult{T}"/> containing an <see cref="AuthResponseDto"/> with the authentication token
        /// if the login is successful; otherwise, an unauthorized result with an error message.</returns>
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> LoginAsync(LoginDto dto)
        {
            try
            {
                var jwt = await authService.LoginAsync(dto);
                return Ok(jwt);
            }
            catch (ArgumentException ex)
            {
                return Unauthorized(ex.Message);
            }
        }

        /// <summary>
        /// Generates a new authentication token pair using the provided refresh token.
        /// </summary>
        /// <param name="dto">An object containing the refresh token and related information required to obtain new authentication tokens.
        /// Cannot be null.</param>
        /// <returns>An <see cref="ActionResult{T}"/> containing a new <see cref="AuthResponseDto"/> with refreshed
        /// authentication tokens if the refresh token is valid; otherwise, an unauthorized response.</returns>
        [HttpPost("refresh")]
        public async Task<ActionResult<AuthResponseDto>> RefreshAsync(RefreshTokenDto dto)
        {
            try
            {
                var jwt = await authService.RefreshAsync(dto);
                return Ok(jwt);
            }
            catch (ArgumentException ex)
            {
                return Unauthorized(ex.Message);
            }
        }

        /// <summary>
        /// Updates the signed-in user's password using the specified password information.
        /// </summary>
        /// <remarks>This action requires the user to be authenticated. The password update will fail if
        /// the provided current password is incorrect or if the new password does not meet the required
        /// criteria.</remarks>
        /// <param name="dto">An object containing the current and new password details required to update the user's password. Cannot be
        /// null.</param>
        /// <returns>An <see cref="ActionResult"/> indicating the result of the password update operation. Returns <see
        /// cref="OkObjectResult"/> if the password is updated successfully; <see cref="UnauthorizedResult"/> if the
        /// user is not authenticated; or <see cref="BadRequestObjectResult"/> if the input is invalid.</returns>
        [HttpPut("updatePassword"), Authorize]
        public async Task<ActionResult> UpdatePasswordAsync(UpdatePasswordDto dto)
        {
            try
            {
                var user = await GetSignedInUserAsync();
                if (user == null) return Unauthorized("User not authenticated.");
                await authService.UpdateUserPasswordAsync(user, dto);
                return Ok("Password updated successfully.");
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Updates the signed-in user's description with the specified information.
        /// </summary>
        /// <param name="dto">An object containing the new description details to apply to the user profile. Cannot be null.</param>
        /// <returns>An <see cref="ActionResult"/> indicating the result of the update operation. Returns 200 OK if the update is
        /// successful, 401 Unauthorized if the user is not authenticated, or 400 Bad Request if the input is invalid.</returns>
        [HttpPut("updateDescription"), Authorize]
        public async Task<ActionResult> UpdateDescriptionAsync(UpdateDescriptionDto dto)
        {
            try 
            {
                var user = await GetSignedInUserAsync();
                if (user == null) return Unauthorized("User not authenticated.");
                await authService.UpdateUserDescriptionAsync(user, dto);
                return Ok("Description updated successfully.");
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Retrieves information about the currently authenticated user.
        /// </summary>
        /// <returns>An <see cref="ActionResult{MeResponseDto}"/> containing the current user's profile information if the user
        /// is authenticated; otherwise, a 500 Internal Server Error result if the user cannot be retrieved.</returns>
        [HttpGet("me"), Authorize]
        public async Task<ActionResult<MeResponseDto>> GetMeAsync()
        {
            //Get the signed-in user
            var user = await GetSignedInUserAsync();
            if (user == null) return StatusCode(500);

            //Map to MeDTO
            var meDto = MeResponseDto.FromUser(user);

            return Ok(meDto);
        }

		//     [HttpPost("OauthLogin")]
		//     public async Task<ActionResult> OauthLoginAsync(OauthDto dto)
		//     {
		//         if (dto == null) return BadRequest("Invalid request data.");
		//         if (string.IsNullOrWhiteSpace(dto.Provider)) return BadRequest("Provider is required.");
		//         if (string.IsNullOrWhiteSpace(dto.Code)) return BadRequest("Code is required.");

		//         if (dto.Provider == "Google")
		//         {
		//             using var client = new HttpClient();

		//             client.DefaultRequestHeaders.Authorization =
		//                 new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", dto.Code);

		//             var response = await client.GetAsync("https://www.googleapis.com/oauth2/v2/userinfo");

		//             if (!response.IsSuccessStatusCode) return BadRequest(await response.Content.ReadAsStringAsync());

		//             var json = await response.Content.ReadAsStringAsync();

		//             var jsonDoc = System.Text.Json.JsonDocument.Parse(json);
		//             return Ok(jsonDoc);
		//}

		//         return BadRequest("Provider not supported.!!!!!!!!!");
		//     }

		[HttpPost("oauth/google/callback")]
		public async Task<IActionResult> GoogleCallback([FromBody] GoogleCodeRequest request)
		{
			var client = new HttpClient();

			var dict = new Dictionary<string, string>
	        {
		        { "code", request.Code },
		        { "client_id", configuration["Oauth:Google:ClientId"] },
		        { "client_secret", configuration["Oauth:Google:ClientSecret"] },
		        { "redirect_uri", configuration["Oauth:Google:RedirectUri"] },
		        { "grant_type", "authorization_code" }
	        };

			var response = await client.PostAsync(
				"https://oauth2.googleapis.com/token",
				new FormUrlEncodedContent(dict));

			var json = await response.Content.ReadAsStringAsync();
			var token = JsonSerializer.Deserialize<GoogleTokenResponse>(json);

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

            var user = await DbContext.Users.FirstOrDefaultAsync(u => u.Email == googleUser.Email);

			if (user == null)
            {
                user = new User
                {
                    Email = googleUser.Email,
                    FirstName = googleUser.FirstName,
                    LastName = googleUser.LastName,
                    Birthday = DateOnly.FromDateTime(DateTime.UtcNow), // Placeholder, since Google doesn't provide birthday by default
					HashedPassword = string.Empty, // No password since it's OAuth
                    IsEmailVerified = true // Assume verified since it's from Google
                };
                DbContext.Users.Add(user);
                await DbContext.SaveChangesAsync();
            }

            var authResponse = await jwtService.CreateAuthResponseAsync(user);

			return Ok(authResponse);
		}

		public class GoogleCodeRequest
		{
			public string Code { get; set; }
		}

		public class GoogleTokenResponse
		{
			public string access_token { get; set; }
			public string id_token { get; set; }
			public string token_type { get; set; }
			public int expires_in { get; set; }
			public string refresh_token { get; set; } // optional, may be null
		}
	}
}