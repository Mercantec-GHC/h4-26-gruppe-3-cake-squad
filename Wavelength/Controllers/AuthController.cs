using Commons.Dtos;
using Commons.Models.DatabaseModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using Wavelength.Data;
using Wavelength.Services;

namespace Wavelength.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext dbContext;
        private readonly JwtService jwtService;

        public AuthController(AppDbContext dbContext, JwtService jwtService)
        {
            this.dbContext = dbContext;
            this.jwtService = jwtService;
        }

        /// <summary>
        /// Registers a new user account with the provided registration details.
        /// </summary>
        /// <remarks>The user must be at least 18 years old, and the email address must be unique and in a
        /// valid format. The password must be at least 8 characters long and meet complexity requirements. This action
        /// does not authenticate the user after registration.</remarks>
        /// <param name="dto">An object containing the user's registration information, including first name, last name, email address,
        /// password, and birthday. All fields are required.</param>
        /// <returns>An HTTP 200 OK result containing the created user if registration is successful; otherwise, a Bad Request
        /// result with an error message describing the validation failure.</returns>
        [HttpPost("register")]
        public async Task<ActionResult> RegisterAsync(RegisterDto dto)
        {
            //Input validation
            if (string.IsNullOrWhiteSpace(dto.FirstName)) return BadRequest("First name is required.");
            if (string.IsNullOrWhiteSpace(dto.LastName)) return BadRequest("Last name is required.");
            if (!Regex.Matches(dto.Email, "^[\\w-\\.]+@([\\w-]+\\.)+[\\w-]{2,4}$").Any()) return BadRequest ("Invalid email format.");
            if (dto.Password.Length < 8) return BadRequest("Password must be at least 8 characters long.");
            if (!IsPasswordSecure(dto.Password)) return BadRequest("Password must contain at least one uppercase letter, one lowercase letter, one digit, and one special character.");
            if (dto.Birthday >= DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-18))) return BadRequest("You must be at least 18 years old to register.");
            if (dbContext.Users.Any(u => u.Email == dto.Email)) return BadRequest("Email already in use.");

            var user = new User
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = dto.Email.ToLower(),
                Birthday = dto.Birthday,
                HashedPassword = BCrypt.Net.BCrypt.HashPassword(dto.Password), 
                Description = string.Empty
            };
   
            await dbContext.Users.AddAsync(user);
            await dbContext.SaveChangesAsync();

            return Ok(user);
        }

        /// <summary>
        /// Authenticates a user with the specified credentials and returns a JWT-based authentication response if
        /// successful.
        /// </summary>
        /// <param name="dto">The login credentials, including the user's email address and password.</param>
        /// <returns>An <see cref="ActionResult{T}"/> containing an <see cref="AuthResponseDto"/> with authentication details if
        /// the login is successful; otherwise, an unauthorized result if the credentials are invalid.</returns>
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> LoginAsync(LoginDto dto)
        {
            //Find user with email and include UserRoles for JWT claims
            var user = await dbContext.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Email == dto.Email.ToLower());

            if (user == null)
                return Unauthorized("Invalid email or password.");

            //Verify password
            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.HashedPassword))
                return Unauthorized("Invalid email or password.");

            return Ok(await jwtService.CreateAuthResponseAsync(user));
        }

        /// <summary>
        /// Generates a new authentication response using a valid refresh token.
        /// </summary>
        /// <param name="dto">An object containing the refresh token to validate and use for generating a new authentication response.
        /// Cannot be null.</param>
        /// <returns>An <see cref="ActionResult{T}"/> containing an <see cref="AuthResponseDto"/> if the refresh token is valid;
        /// otherwise, an unauthorized result if the token is invalid or expired.</returns>
        [HttpPost("refresh")]
        public async Task<ActionResult<AuthResponseDto>> RefreshAsync(RefreshTokenDto dto)
        {
            var user = await jwtService.ValidateRefreshTokenAsync(dto.Token);
            if (user == null) return Unauthorized("Invalid or expired refresh token.");

            return Ok(await jwtService.CreateAuthResponseAsync(user));
        }

        /// <summary>
		/// Checks if the password is secure, using <see cref="Regex"/>.
		/// </summary>
		/// <param name="password"></param>
		/// <returns><see cref="bool"/> of true, if the given password is secure.</returns>
		private bool IsPasswordSecure(string password)
        {
            if (string.IsNullOrEmpty(password)) return false;

            // Regex to check if the password meets the following criteria:
            // ^               - Ensures the match starts at the beginning of the string.
            // (?=.*[A-Z])     - Asserts that there is at least one uppercase letter in the string.
            // (?=.*[a-z])     - Asserts that there is at least one lowercase letter in the string.
            // (?=.*\d)        - Asserts that there is at least one digit (number) in the string.
            // (?=.*[\W_])     - Asserts that there is at least one special character (non-word character or underscore).
            // [^\s]{8,}       - Ensures the string is at least 8 characters long and does not contain any whitespace.
            // $               - Ensures the match ends at the end of the string.
            var regex = new Regex(@"^(?=.*[A-Z])(?=.*[a-z])(?=.*\d)(?=.*[\W_])[^\s]{8,}$");
            return regex.IsMatch(password);
        }
    }
}