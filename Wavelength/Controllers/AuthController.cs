using Commons.Dtos;
using Commons.Models;
using Microsoft.AspNetCore.Mvc;
using Wavelength.Data;

namespace Wavelength.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext dbContext;

        public AuthController(AppDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        [HttpPost("register")]
        public async Task<ActionResult> RegisterAsync(RegisterDto dto)
        {
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
    }
}