using Commons.Enums;
using Commons.Models.Database;
using Commons.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Wavelength.Data;

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

		[HttpPut("SetTags")]
		public async Task<ActionResult> SetTags(List<TagsEnum> tags)
		{
			var user = await GetSignedInUserAsync();
			if (user == null) return StatusCode(500);

			int maxTags = 10;
			if (tags.Count > maxTags) return BadRequest($"You can only have up to {maxTags} tags.");

            user.ValueTags = tags;
			await DbContext.SaveChangesAsync();

            return Ok(tags);
		}
	}
}