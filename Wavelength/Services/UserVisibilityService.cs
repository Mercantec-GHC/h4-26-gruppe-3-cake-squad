using Commons.Enums;
using Commons.Models.Database;
using Commons.Models.Dtos;
using Microsoft.EntityFrameworkCore;
using Wavelength.Data;

namespace Wavelength.Services
{
	/// <summary>
	/// Provides methods to manage user visibility settings within the application, including setting, deleting, blocking,
	/// and unblocking visibility between users.
	/// </summary>
	/// <remarks>This service requires a properly configured database context to function correctly. It handles
	/// visibility records and ensures that user interactions are managed according to specified visibility
	/// rules.</remarks>
	public class UserVisibilityService
	{
		private readonly AppDbContext dbContext;

		/// <summary>
		/// Initializes a new instance of the UserVisibilityService class using the specified database context.
		/// </summary>
		/// <remarks>This service manages user visibility settings within the application. Ensure that the provided
		/// database context is properly configured before instantiating this service.</remarks>
		/// <param name="dbContext">The database context used to access and manage application data. This parameter cannot be null.</param>
		public UserVisibilityService(AppDbContext dbContext)
		{
			this.dbContext = dbContext; 
		}

		/// <summary>
		/// Sets the visibility status between two users based on the specified visibility request.
		/// </summary>
		/// <remarks>If a visibility record between the specified users does not exist, this method creates a new
		/// record. Both user IDs must refer to existing users in the database.</remarks>
		/// <param name="dto">A request object that specifies the source user ID, target user ID, and the desired visibility status to be set.</param>
		/// <returns>A task that represents the asynchronous operation.</returns>
		/// <exception cref="InvalidOperationException">Thrown when the provided visibility status cannot be parsed to a valid enumeration value.</exception>
		/// <exception cref="KeyNotFoundException">Thrown when either the source user or the target user specified in the request does not exist.</exception>
		public async Task SetUserVisibilityAsync(UserVisibilityRequestDto dto)
		{
			UserVisibilityEnum visibility;
			if (!Enum.TryParse<UserVisibilityEnum>(dto.VisibilityEnum, true, out visibility)) throw new InvalidOperationException("Parsing of enum went wrong.");

			if (!await dbContext.Users.AnyAsync(u => u.Id == dto.TargetUserId || u.Id == dto.SourceUserId)) throw new KeyNotFoundException("No visibilties with the users id was found.");

			var userVisibility = await dbContext.UserVisibilities
				.FirstOrDefaultAsync(uv => uv.TargetUserId == dto.TargetUserId &&
					uv.SourceUserId == dto.SourceUserId);
			if (userVisibility == null)
			{
				userVisibility = new UserVisibility
				{
					SourceUserId = dto.SourceUserId,
					TargetUserId = dto.TargetUserId
				};
				await dbContext.UserVisibilities.AddAsync(userVisibility);
			}

			userVisibility.Visibility = visibility;
			await dbContext.SaveChangesAsync();
		}

		/// <summary>
		/// Deletes the user visibility record associated with the specified visibility ID.
		/// </summary>
		/// <remarks>This method permanently removes the user visibility from the database. Ensure that the visibility
		/// ID provided corresponds to an existing record.</remarks>
		/// <param name="visibilityId">The unique identifier of the user visibility to be deleted. Must be a valid ID of an existing user visibility.</param>
		/// <returns></returns>
		/// <exception cref="KeyNotFoundException">Thrown if no user visibility is found with the specified visibility ID.</exception>
		public async Task DeleteUserVisibilityAsync(int visibilityId)
		{
			var userVisibility = await dbContext.UserVisibilities.FirstOrDefaultAsync(uv => uv.Id == visibilityId);
			if (userVisibility == null) throw new KeyNotFoundException("No user visibility found.");

			dbContext.UserVisibilities.Remove(userVisibility);
			await dbContext.SaveChangesAsync();
		}

		/// <summary>
		/// Blocks the specified user, preventing them from interacting with the calling user.
		/// </summary>
		/// <remarks>If the target user is not already blocked, a new blocking rule is created and persisted. Blocking
		/// a user prevents further interactions between the calling user and the blocked user.</remarks>
		/// <param name="targetId">The unique identifier of the user to be blocked.</param>
		/// <param name="user">The user who is initiating the block action.</param>
		/// <returns>A task that represents the asynchronous operation of blocking the user.</returns>
		/// <exception cref="InvalidOperationException">Thrown if the target user is already blocked by the calling user.</exception>
		public async Task BlockUserAsync(string targetId, User user)
		{
			var userVisibility = user.UserVisibilities.FirstOrDefault(uv => uv.TargetUserId == targetId);
			if (userVisibility != null)
			{
				if (userVisibility.Visibility == UserVisibilityEnum.Blocked) throw new InvalidOperationException("The target user is already blocked.");

				userVisibility.Visibility = UserVisibilityEnum.Blocked;

				await dbContext.SaveChangesAsync();
				return;
			}

			var newRule = new UserVisibility
			{
				SourceUserId = user.Id,
				TargetUserId = targetId,
				Visibility = UserVisibilityEnum.Blocked
			};

			dbContext.UserVisibilities.Add(newRule);
			await dbContext.SaveChangesAsync();
		}

		/// <summary>
		/// Updates the visibility status of the specified target user to dismissed for the given source user.
		/// </summary>
		/// <remarks>If no existing visibility record is found for the target user, a new record is created with the
		/// dismissed status.</remarks>
		/// <param name="targetId">The unique identifier of the user whose visibility status is to be updated.</param>
		/// <param name="user">The user object representing the source user initiating the dismissal.</param>
		/// <returns>A task that represents the asynchronous operation.</returns>
		/// <exception cref="InvalidOperationException">Thrown if the target user is already dismissed or blocked by the source user.</exception>
		public async Task DismissUserAsync(string targetId, User user)
		{
			var userVisibility = user.UserVisibilities.FirstOrDefault(uv => uv.TargetUserId == targetId);
			if (userVisibility != null)
			{
				if (userVisibility.Visibility == UserVisibilityEnum.Dismissed ||
					userVisibility.Visibility == UserVisibilityEnum.Blocked
				) throw new InvalidOperationException($"The target user is already {userVisibility.Visibility.ToString()}.");

				userVisibility.Visibility = UserVisibilityEnum.Dismissed;

				await dbContext.SaveChangesAsync();
				return;
			}

			var newRule = new UserVisibility
			{
				SourceUserId = user.Id,
				TargetUserId = targetId,
				Visibility = UserVisibilityEnum.Dismissed
			};

			dbContext.UserVisibilities.Add(newRule);
			await dbContext.SaveChangesAsync();
		}

		/// <summary>
		/// Unblocks a previously blocked user, making the specified user visible to the current user.
		/// </summary>
		/// <remarks>This method updates the visibility status of the target user to visible and persists the change
		/// to the database.</remarks>
		/// <param name="targetId">The unique identifier of the user to unblock.</param>
		/// <param name="user">The user object representing the current user performing the unblock operation.</param>
		/// <returns></returns>
		/// <exception cref="KeyNotFoundException">Thrown if the specified target user does not have a visibility setting for the current user.</exception>
		/// <exception cref="InvalidOperationException">Thrown if the specified target user is not currently blocked by the current user.</exception>
		public async Task UnblockUserAsync(string targetId, User user)
		{
			var userVisibility = user.UserVisibilities.FirstOrDefault(uv => uv.TargetUserId == targetId);
			if (userVisibility == null) throw new KeyNotFoundException("Target user has no set visibility.");
			if (userVisibility.Visibility != UserVisibilityEnum.Blocked) throw new InvalidOperationException("Target user is not blocked.");

			userVisibility.Visibility = UserVisibilityEnum.Visible;
			await dbContext.SaveChangesAsync();
		}
	}
}