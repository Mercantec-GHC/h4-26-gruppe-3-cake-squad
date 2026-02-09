using Commons.Enums;
using Commons.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wavelength.Data;

namespace Wavelength.Controllers
{
	/// <summary>
	/// Provides API endpoints for managing the visibility settings of users relative to the currently signed-in user.
	/// </summary>
	/// <remarks>This controller requires authentication for its operations. It enables clients to set or update the
	/// visibility level of a target user, which determines how that user is seen by the current user within the
	/// application. All actions are subject to authorization and input validation.</remarks>
	[ApiController]
	[Route("[controller]")]
	public class UserVisibilityController : BaseController
	{
		/// <summary>
		/// Initializes a new instance of the UserVisibilityController class using the specified database context.
		/// </summary>
		/// <param name="dbContext">The database context to be used for data access operations. Cannot be null.</param>
		public UserVisibilityController(AppDbContext dbContext) : base(dbContext) { }

		/// <summary>
		/// Sets the visibility level of a target user as seen by a source user.
		/// </summary>
		/// <remarks>This action is restricted to users with the 'admin' role. If a visibility record between the
		/// specified users does not exist, a new record is created.</remarks>
		/// <param name="dto">An object containing the source user ID, target user ID, and the desired visibility level. All fields must be
		/// provided and non-empty.</param>
		/// <returns>An HTTP 200 OK result if the visibility is set successfully; otherwise, a 400 Bad Request or 404 Not Found result
		/// if the input is invalid or users are not found.</returns>
		[HttpPost("admin/set"), Authorize(Roles = "admin")]
		public async Task<ActionResult> SetUserVisibilityAsync(UserVisibilityRequestDto dto)
		{
			// Validate input
			if (dto == null) return BadRequest("Request body can not be null.");
			if (string.IsNullOrWhiteSpace(dto.SourceUserId)) return BadRequest("Source user id can not be empty.");
			if (string.IsNullOrWhiteSpace(dto.TargetUserId)) return BadRequest("Target user id can not be empty.");
			if (string.IsNullOrWhiteSpace(dto.VisibilityEnum)) return BadRequest("Visibility cna not be empty.");

			UserVisibilityEnum visibility;
			if (!Enum.TryParse<UserVisibilityEnum>(dto.VisibilityEnum, true, out visibility)) return BadRequest("Failed to parse visibility.");

			if (!await DbContext.Users.AnyAsync(u => u.Id == dto.TargetUserId || u.Id == dto.SourceUserId)) return NotFound();

			// Update or create visibility record
			var userVisibility = await DbContext.UserVisibilities
				.FirstOrDefaultAsync(uv => uv.TargetUserId == dto.TargetUserId &&
					uv.SourceUserId == dto.SourceUserId);
			if (userVisibility == null)
			{
				userVisibility = new UserVisibility
				{
					SourceUserId = dto.SourceUserId,
					TargetUserId = dto.TargetUserId
				};
				await DbContext.UserVisibilities.AddAsync(userVisibility);
			}

			userVisibility.Visibility = visibility;
			await DbContext.SaveChangesAsync();

			return Ok();
		}

		/// <summary>
		/// Deletes the user visibility entry with the specified identifier.
		/// </summary>
		/// <param name="visibilityId">The unique identifier of the user visibility entry to delete. Must be a non-zero value.</param>
		/// <returns>An <see cref="ActionResult"/> indicating the result of the delete operation. Returns <see cref="OkResult"/> if the
		/// entry was deleted successfully, <see cref="NotFoundResult"/> if the entry does not exist, or <see
		/// cref="BadRequestObjectResult"/> if the identifier is invalid.</returns>
		[HttpDelete("admin/delete"), Authorize(Roles = "admin")]
		public async Task<ActionResult> DeleteUserVisibilityAsync(int visibilityId)
		{
			// Validate input.
			if (visibilityId == 0) return BadRequest("Visibility id can not be empty.");

			// Fetches the user visibility.
			var userVisibility = await DbContext.UserVisibilities.FirstOrDefaultAsync(uv => uv.Id == visibilityId);
			if (userVisibility == null) return NotFound();

			DbContext.UserVisibilities.Remove(userVisibility);
			await DbContext.SaveChangesAsync();

			return Ok();
		}

		/// <summary>
		/// Blocks the specified user for the currently signed-in user.
		/// </summary>
		/// <param name="targetId">The unique identifier of the user to be blocked. Cannot be null, empty, or consist only of white-space characters.</param>
		/// <returns>An <see cref="ActionResult"/> indicating the result of the block operation. Returns <see cref="OkResult"/> if the
		/// user was successfully blocked; <see cref="BadRequestObjectResult"/> if the target user is already blocked or if
		/// the target ID is invalid; <see cref="NotFoundResult"/> if the signed-in user is not found.</returns>
		[HttpPost("Block"), Authorize]
		public async Task<ActionResult> BlockUserAsync(string targetId)
		{
			// Validate input.
			if (string.IsNullOrWhiteSpace(targetId)) return BadRequest("Target user id can not be empty.");

			var user = await GetSignedInUserAsync(q => q.Include(u => u.UserVisibilities));
			if (user == null) return StatusCode(500);

			// Logic for existing UserVisibility.
			var userVisibility = user.UserVisibilities.FirstOrDefault(uv => uv.TargetUserId == targetId);
			if (userVisibility != null)
			{
				if (userVisibility.Visibility == UserVisibilityEnum.Blocked) return BadRequest("Target user is already blocked.");

				userVisibility.Visibility = UserVisibilityEnum.Blocked;

				await DbContext.SaveChangesAsync();
				return Ok();
			}

			// Logic for new UserVisibility.
			var newRule = new UserVisibility
			{
				SourceUserId = user.Id,
				TargetUserId = targetId,
				Visibility = UserVisibilityEnum.Blocked
			};

			DbContext.UserVisibilities.Add(newRule);
			await DbContext.SaveChangesAsync();

			return Ok();
		}

		/// <summary>
		/// Marks the specified user as dismissed by the currently signed-in user.
		/// </summary>
		/// <remarks>Use this method to prevent the specified user from appearing in the signed-in user's visibility
		/// list. The dismissed state persists until changed by another action. Requires authentication.</remarks>
		/// <param name="targetId">The unique identifier of the user to be dismissed. Cannot be null, empty, or whitespace.</param>
		/// <returns>An <see cref="ActionResult"/> indicating the outcome of the operation. Returns <see cref="OkResult"/> if the user
		/// is successfully dismissed; <see cref="BadRequestResult"/> if the target user ID is invalid or the user is already
		/// dismissed or blocked; <see cref="StatusCodeResult"/> with status 500 if the signed-in user cannot be retrieved.</returns>
		[HttpPost("Dismissed"), Authorize]
		public async Task<ActionResult> DismissUserAsync(string targetId)
		{
			// Validate input.
			if (string.IsNullOrWhiteSpace(targetId)) return BadRequest("Target user id can not be empty.");

			var user = await GetSignedInUserAsync(q => q.Include(u => u.UserVisibilities));
			if (user == null) return StatusCode(500);

			// Logic for existing UserVisibility.
			var userVisibility = user.UserVisibilities.FirstOrDefault(uv => uv.TargetUserId == targetId);
			if (userVisibility != null)
			{
				if (userVisibility.Visibility == UserVisibilityEnum.Dismissed || 
					userVisibility.Visibility == UserVisibilityEnum.Blocked
				) return BadRequest($"Target user is already {userVisibility.Visibility.ToString()}.");

				userVisibility.Visibility = UserVisibilityEnum.Dismissed;

				await DbContext.SaveChangesAsync();
				return Ok();
			}

			// Logic for new UserVisibility.
			var newRule = new UserVisibility
			{
				SourceUserId = user.Id,
				TargetUserId = targetId,
				Visibility = UserVisibilityEnum.Dismissed
			};

			DbContext.UserVisibilities.Add(newRule);
			await DbContext.SaveChangesAsync();
			return Ok();
		}

		/// <summary>
		/// Unblocks the specified user for the currently signed-in user.
		/// </summary>
		/// <param name="targetId">The unique identifier of the user to unblock. Cannot be null, empty, or whitespace.</param>
		/// <returns>An <see cref="ActionResult"/> indicating the result of the unblock operation. Returns <see cref="OkResult"/> if
		/// the user was successfully unblocked; otherwise, returns a <see cref="BadRequestObjectResult"/> if the target user
		/// is not blocked or has no visibility settings, or a <see cref="StatusCodeResult"/> with status code 500 if the
		/// signed-in user cannot be retrieved.</returns>
		[HttpPost("Unblock"), Authorize]
		public async Task<ActionResult> UnblockUserAsync(string targetId)
		{
			// Validate input.
			if (string.IsNullOrWhiteSpace(targetId)) return BadRequest("Target user id can not be empty.");

			var user = await GetSignedInUserAsync(q =>  q.Include(u => u.UserVisibilities));
			if (user == null) return StatusCode(500);

			// Logic for existing UserVisibility.
			var userVisibility = user.UserVisibilities.FirstOrDefault(uv => uv.TargetUserId == targetId);
			if (userVisibility == null) return BadRequest("Target user has no visibility settings.");
			if (userVisibility.Visibility != UserVisibilityEnum.Blocked) return BadRequest("Target user is not blocked.");

			userVisibility.Visibility = UserVisibilityEnum.Visible;
			await DbContext.SaveChangesAsync();

			return Ok();
		}
	}
}