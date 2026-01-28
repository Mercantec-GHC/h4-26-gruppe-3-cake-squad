using Commons.Dtos;
using Commons.Models;
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
                Email = dto.Email,
                Birthday = dto.Birthday,
                HashedPassword = BCrypt.Net.BCrypt.HashPassword(dto.Password), 
                Description = string.Empty
            };
   
            await dbContext.Users.AddAsync(user);
            await dbContext.SaveChangesAsync();

            return Ok(user);
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