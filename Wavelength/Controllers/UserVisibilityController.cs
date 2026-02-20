using Commons.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wavelength.Data;
using Wavelength.Services;

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
		private readonly UserVisibilityService userVisibility;
		private readonly UserVisibilityService visiblityService;

		/// <summary>
		/// Initializes a new instance of the UserVisibilityController class using the specified database context.
		/// </summary>
		/// <param name="dbContext">The database context to be used for data access operations. Cannot be null.</param>
		public UserVisibilityController(AppDbContext dbContext, UserVisibilityService visiblityService) : base(dbContext)
		{
			this.visiblityService = visiblityService;
		}

		/// <summary>
		/// Sets the visibility status of a target user based on the specified request data.
		/// </summary>
		/// <remarks>This method requires the caller to be authenticated with the 'Admin' role. The request will fail
		/// if any required fields are missing or invalid.</remarks>
		/// <param name="dto">A UserVisibilityRequestDto that contains the source user ID, target user ID, and the desired visibility status.
		/// This parameter cannot be null, and all fields must be provided.</param>
		/// <returns>An ActionResult that indicates the outcome of the operation. Returns an HTTP 200 OK response if the visibility is
		/// set successfully; otherwise, returns a BadRequest with an error message.</returns>
		[HttpPost("admin/set"), Authorize(Roles = "Admin")]
		public async Task<ActionResult> SetUserVisibilityAsync(UserVisibilityRequestDto dto)
		{
			try 
			{ 
				if (dto == null) return BadRequest("Request body can not be null.");
				if (string.IsNullOrWhiteSpace(dto.SourceUserId)) return BadRequest("Source user id can not be empty.");
				if (string.IsNullOrWhiteSpace(dto.TargetUserId)) return BadRequest("Target user id can not be empty.");
				if (string.IsNullOrWhiteSpace(dto.VisibilityEnum)) return BadRequest("Visibility cna not be empty.");
				
				await visiblityService.SetUserVisibilityAsync(dto);
				return Ok();
			}
			catch (Exception ex)
			{
				return BadRequest($"Failed to set visibility: {ex.Message}");
			}
		}

		/// <summary>
		/// Deletes the user visibility setting associated with the specified visibility ID.
		/// </summary>
		/// <remarks>This method requires the caller to have 'Admin' role permissions. If the visibility ID is zero, a
		/// BadRequest response is returned.</remarks>
		/// <param name="visibilityId">The ID of the visibility setting to delete. Must be a non-zero integer.</param>
		/// <returns>An ActionResult that indicates the outcome of the delete operation. Returns Ok if the deletion is successful;
		/// otherwise, returns a BadRequest with an error message.</returns>
		[HttpDelete("admin/delete"), Authorize(Roles = "Admin")]
		public async Task<ActionResult> DeleteUserVisibilityAsync(int visibilityId)
		{
			try
			{
				if (visibilityId == 0) return BadRequest("Visibility id can not be empty.");

				await visiblityService.DeleteUserVisibilityAsync(visibilityId);
				return Ok();
			}
			catch (Exception ex)
			{
				return BadRequest($"Failed to set visibility: {ex.Message}");
			}
		}

		/// <summary>
		/// Blocks the specified user, preventing them from interacting with the signed-in user.
		/// </summary>
		/// <remarks>This method requires the caller to be authenticated. If the target user ID is invalid or the
		/// operation fails, appropriate error responses are returned.</remarks>
		/// <param name="targetId">The unique identifier of the user to be blocked. This parameter cannot be null or empty.</param>
		/// <returns>An ActionResult indicating the outcome of the block operation. Returns Ok() if the user is successfully blocked;
		/// otherwise, returns a BadRequest with an error message.</returns>
		[HttpPost("Block"), Authorize]
		public async Task<ActionResult> BlockUserAsync(string targetId)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(targetId)) return BadRequest("Target user id can not be empty.");

				var user = await GetSignedInUserAsync(q => q.Include(u => u.UserVisibilities));
				if (user == null) return StatusCode(500);

				await visiblityService.BlockUserAsync(targetId, user);
				return Ok();
				
			}
			catch (Exception ex)
			{
				return BadRequest($"Failed to set visibility: {ex.Message}");
			}
		}

		/// <summary>
		/// Dismisses the specified user from the signed-in user's visibility list.
		/// </summary>
		/// <remarks>This method requires the caller to be authenticated. If the specified user identifier is invalid
		/// or the operation fails, an appropriate error response is returned.</remarks>
		/// <param name="targetId">The unique identifier of the user to be dismissed. This parameter cannot be null, empty, or consist only of
		/// white-space characters.</param>
		/// <returns>An <see cref="ActionResult"/> that indicates the result of the operation. Returns <see cref="OkResult"/> if the
		/// user is successfully dismissed; otherwise, returns a <see cref="BadRequestObjectResult"/> with an error message.</returns>
		[HttpPost("Dismissed"), Authorize]
		public async Task<ActionResult> DismissUserAsync(string targetId)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(targetId)) return BadRequest("Target user id can not be empty.");

				var user = await GetSignedInUserAsync(q => q.Include(u => u.UserVisibilities));
				if (user == null) return StatusCode(500);

				await visiblityService.DismissUserAsync(targetId, user);
				return Ok();
			}
			catch(Exception ex)
			{
				return BadRequest($"Failed to set visibility: {ex.Message}");
			}
		}

		/// <summary>
		/// Unblocks the user specified by the target identifier, allowing them to regain access to the system.
		/// </summary>
		/// <remarks>This action requires the caller to be authorized. If the specified user identifier is invalid or
		/// the operation fails, an appropriate error response is returned.</remarks>
		/// <param name="targetId">The unique identifier of the user to be unblocked. This parameter cannot be null, empty, or consist only of
		/// white-space characters.</param>
		/// <returns>An <see cref="ActionResult"/> that indicates the result of the operation. Returns <see cref="OkResult"/> if the
		/// user is successfully unblocked; otherwise, returns a <see cref="BadRequestObjectResult"/> with an error message.</returns>
		[HttpPost("Unblock"), Authorize]
		public async Task<ActionResult> UnblockUserAsync(string targetId)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(targetId)) return BadRequest("Target user id can not be empty.");

				var user = await GetSignedInUserAsync(q =>  q.Include(u => u.UserVisibilities));
				if (user == null) return StatusCode(500);

				await visiblityService.UnblockUserAsync(targetId, user);
				return Ok();
			}
			catch (Exception ex)
			{
				return BadRequest($"Failed to unblock user: {ex.Message}");
			}
		}
	}
}