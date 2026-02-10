using Commons.Enums;
using Commons.Models.Database;
using Commons.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using System;
using System.Security.Claims;
using Wavelength.Data;

namespace Wavelength.Controllers
{
	[ApiController]
	[Route("[controller]")]
	public class UserController : BaseController
	{
		public UserController(AppDbContext dbContext) : base(dbContext) { }

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
				Description = user.Description,
                Tags = user.ValueTags.Select(t => t.ToString()).ToList()
            };

			return Ok(userDto);
		}

		/// <summary>
		/// Retrieves a list of all available tag names defined in the TagsEnum.
		/// </summary>
		/// <remarks>This method is intended for clients that need to enumerate all possible tags supported by the
		/// application. The returned tag names correspond to the values of the TagsEnum and can be used for filtering,
		/// selection, or display purposes.</remarks>
		/// <returns>An ActionResult containing a list of strings representing all tag names. The list will be empty if no tags are
		/// defined.</returns>
		[HttpPost("AllTags"), Authorize]
		public ActionResult<List<string>> GetAllTags()
		{
			// Get all tag names from the TagsEnum and return them as a list of strings
			var tags = Enum.GetValues<TagsEnum>()
				.Select(t => t.ToString())
				.ToList();

			return Ok(tags);
		}

		/// <summary>
		/// Updates the signed-in user's tag list with the specified tags.
		/// </summary>
		/// <remarks>Tag names are case-insensitive and must match defined values in TagsEnum. Only valid tags are
		/// assigned; invalid tag names are ignored. The method requires the user to be authenticated.</remarks>
		/// <param name="tags">A list of tag names to assign to the user. Each tag must correspond to a valid value in the TagsEnum. The list can
		/// contain up to 10 tags.</param>
		/// <returns>An ActionResult indicating the outcome of the operation. Returns 204 (No Content) if the tags are updated
		/// successfully; returns 400 (Bad Request) if the tag limit is exceeded; returns 500 (Internal Server Error) if the
		/// user is not signed in.</returns>
		[HttpPut("SetTags"), Authorize]
		public async Task<ActionResult> SetTags(List<string> tags)
		{
			var user = await GetSignedInUserAsync();
			if (user == null) return StatusCode(500);

			// Validate and convert string tags to enum values
			List<TagsEnum> valueTags = tags
				.Select(s =>
				{
					bool ok = Enum.TryParse<TagsEnum>(s, true, out var value);
					return (ok, value);
				})
				.Where(x => x.ok)
				.Select(x => x.value)
				.ToList();

			int maxTags = 10;
			if (tags.Count > maxTags) return BadRequest($"You can only have up to {maxTags} tags.");

			user.ValueTags = valueTags;
			await DbContext.SaveChangesAsync();

			return NoContent();
		}

		/// <summary>
		/// Retrieves the list of tags associated with a specified user or the currently signed-in user.
		/// </summary>
		/// <param name="userId">The unique identifier of the user whose tags to retrieve. If null or empty, retrieves tags for the currently
		/// signed-in user.</param>
		/// <returns>An <see cref="ActionResult{T}"/> containing a list of tags for the specified user. Returns a 404 response if the
		/// user is not found, or a 500 response if the signed-in user cannot be determined.</returns>
		[Authorize]
		[HttpGet("Tags")]
		[HttpGet("Tags/{userId}")]
		public async Task<ActionResult<List<string>>> GetUserTags(string? userId)
		{
            // Validate user
            var user = await GetSignedInUserAsync();
            if (user == null) return StatusCode(500);

            // If no userId provided, use signed-in user
            if (!string.IsNullOrEmpty(userId)) user = await DbContext.Users.FindAsync(userId);
            if (user == null) return NotFound("User not found.");

            var tags = user.ValueTags
                .Select(t => t.ToString())
                .ToList();

            return Ok(tags);
		}

		/// <summary>
		/// Discovers a user for the signed-in user to interact with, excluding specified user IDs and users already
		/// discovered.
		/// </summary>
		/// <remarks>This method only returns users who have not already been discovered by the signed-in user and are
		/// not included in the provided exclusion list. The result is randomized to provide varied discovery experiences. The
		/// caller must be authenticated.</remarks>
		/// <param name="userIds">A list of user IDs to exclude from discovery. If null, no users are excluded based on ID.</param>
		/// <returns>An ActionResult containing a DiscoverUserResponseDto with the discovered user's details if found; otherwise, a 404
		/// Not Found result if no suitable user is available.</returns>
		[Authorize]
		[HttpPost("Discover")]
		public async Task<ActionResult<DiscoverUserResponseDto>> DiscoverUsers(List<string>? userIds)
		{
            var user = await GetSignedInUserAsync();
            if (user == null) return StatusCode(500);

			var userResult = await DbContext.Users
				.Include(u => u.UserVisibilities)
				.Include(u => u.ProfilePictures)
				.Where(u => userIds == null || !userIds.Contains(u.Id))
				.Where(u => u.Id != user.Id)
				.Where(u => !u.UserVisibilities.Any(uv => uv.SourceUserId == user.Id))
                .OrderBy(u => Guid.NewGuid())
                .FirstOrDefaultAsync();

			if (userResult == null) return NotFound("No users found.");

            return Ok(new DiscoverUserResponseDto
			{
				Id = userResult.Id,
				FirstName = userResult.FirstName,
				LastName = userResult.LastName,
				Description = userResult.Description,
				Pictures = userResult.ProfilePictures
					.Where(p => p.PictureType == PictureTypeEnum.Interest)
					.OrderBy(_ => Guid.NewGuid())
					.Take(6)
					.Select(p => p.Id)
					.ToList(),
                Tags = userResult.ValueTags
					.Select(t => t.ToString()).ToList()
			});
		}
    }
}