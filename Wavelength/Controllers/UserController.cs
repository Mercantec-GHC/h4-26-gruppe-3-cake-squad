using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wavelength.Data;
using Commons.Models.Dtos;
using Commons.Models.Database;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace Wavelength.Controllers
{
	[ApiController]
	[Route("[controller]")]
	public class UserController : BaseController
	{
		public UserController(AppDbContext dbContext) : base(dbContext) {}

		/// <summary>
		/// Retrieves the details of a user with the specified identifier.
		/// </summary>
		/// <remarks>This endpoint requires authentication. Only authorized users can access user details.</remarks>
		/// <param name="id">The unique identifier of the user to retrieve. Cannot be null or empty.</param>
		/// <returns>An <see cref="ActionResult{UserDto}"/> containing the user details if found; otherwise, a 404 Not Found response.</returns>
		[HttpGet("{id}"), Authorize]
		public async Task<ActionResult<UserResponseDto>> GetUserByIdAsync(string id)
		{
			var user = await DbContext.Users.FirstOrDefaultAsync(u => u.Id == id);
			
			if (user == null) return NotFound("User not found.");
			
			var userDto = new UserResponseDto
			{
				Id = user.Id,
				FirstName = user.FirstName,
				LastName = user.LastName,
				Description = user.Description
			};

			return Ok(userDto);
		}
	}
}