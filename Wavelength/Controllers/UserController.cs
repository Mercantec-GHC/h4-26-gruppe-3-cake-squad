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
	public class UserController : ControllerBase
	{
		private readonly AppDbContext dbContext;

		public UserController(AppDbContext dbContext)
		{
			this.dbContext = dbContext;
		}

		/// <summary>
		/// Retrieves information about the currently authenticated user.
		/// </summary>
		/// <remarks>This endpoint requires authentication. The returned profile includes basic user details such as
		/// ID, name, birthday, and email address.</remarks>
		/// <returns>An <see cref="ActionResult{MeDto}"/> containing the user's profile information if authenticated; otherwise, an
		/// unauthorized response.</returns>
		[HttpGet("me")]
		[Authorize]
		public async Task<ActionResult<MeResponseDto>> GetMeAsync()
		{
			//Get the signed-in user
			var user = await GetSignedInUserAsync();
			if (user == null) return StatusCode(500);

			//Map to MeDTO
			var meDto = new MeResponseDto
			{
				Id = user.Id,
				FirstName = user.FirstName,
				LastName = user.LastName,
				Birthday = user.Birthday,
				Email = user.Email,
				Description = user.Description
			};

			return Ok(meDto);
		}

		/// <summary>
		/// Retrieves the details of a user with the specified identifier.
		/// </summary>
		/// <remarks>This endpoint requires authentication. Only authorized users can access user details.</remarks>
		/// <param name="id">The unique identifier of the user to retrieve. Cannot be null or empty.</param>
		/// <returns>An <see cref="ActionResult{UserDto}"/> containing the user details if found; otherwise, a 404 Not Found response.</returns>
		[HttpGet("{id}")]
		[Authorize]
		public async Task<ActionResult<UserResponseDto>> GetUserByIdAsync(string id)
		{
			var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == id);
			
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

		/// <summary>
		/// Asynchronously retrieves the currently signed-in user, if available.
		/// </summary>
		/// <returns>A <see cref="User"/> object representing the signed-in user, or <see langword="null"/> if no user is signed in or
		/// the user cannot be found.</returns>
		protected async Task<User?> GetSignedInUserAsync()
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (userId == null) return null;

			var user = await dbContext.Users.Where(u => u.Id == userId)
				.Include(u => u.UserRoles)
				.FirstOrDefaultAsync();

			if (user == null) return null;

			return user;
		}
	}
}