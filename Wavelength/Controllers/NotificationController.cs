using Commons.Models.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wavelength.Data;
using Commons.Enums;
using Microsoft.AspNetCore.Authorization;
using Wavelength.Services;

namespace Wavelength.Controllers
{
	/// <summary>
	/// Handles operations related to notifications, including creating notifications for target users and retrieving
	/// notifications for the currently authenticated user.
	/// </summary>
	/// <remarks>This controller requires users to be authenticated, with specific roles for certain actions. It
	/// provides endpoints for both admin users to create notifications and for regular users to fetch their
	/// notifications.</remarks>
	[ApiController]
	[Route("[controller]")]
	public class NotificationController : BaseController
	{
		private readonly NotificationService notificationService;

		/// <summary>
		/// Initializes a new instance of the NotificationController class with the specified database context and
		/// notification service.
		/// </summary>
		/// <remarks>Both the database context and notification service must be provided and properly configured for
		/// the controller to function as expected.</remarks>
		/// <param name="context">The database context used to access and manage application data.</param>
		/// <param name="notificationService">The service responsible for handling notification operations, such as sending and managing notifications.</param>
		public NotificationController(AppDbContext context, NotificationService notificationService) : base(context) 
		{
			this.notificationService = notificationService;
		}

		/// <summary>
		/// Creates a notification for the specified target users using the provided request data.
		/// </summary>
		/// <remarks>This method requires the caller to be authenticated as an Admin. The request is validated to
		/// ensure all required information is provided before creating the notification.</remarks>
		/// <param name="request">An object containing the details of the notification to create, including the list of target user IDs and the
		/// notification content. This parameter must not be null, must specify at least one target user, and must include
		/// non-empty content.</param>
		/// <returns>An ActionResult that indicates the result of the operation. Returns Ok() if the notification is created
		/// successfully; otherwise, returns BadRequest with an error message if validation fails or the request is invalid.</returns>
		[HttpPost, Authorize(Roles = "Admin")]
		public async Task<ActionResult> CreateNotificationAsync(AdminNotificationRequestDto request)
		{
			try 
			{
				if (request == null) return BadRequest("Request body can not be null.");
				if (request.TargetIds == null || request.TargetIds.Count() == 0) return BadRequest("At least one target user must be specified.");
				if (string.IsNullOrWhiteSpace(request.Content)) return BadRequest("Notification content can not be empty.");

				var sender = await GetSignedInUserAsync(q => q.Include(u => u.UserVisibilities));
				if (sender == null) return StatusCode(500);
				if (sender.Roles.Contains(RoleEnum.Admin) == false) return Unauthorized();

				var notification = new NotificationRequestDto
				{
					SenderId = sender.Id,
					TargetIds = request.TargetIds,
					Content = request.Content
				};

				await notificationService.AdminCreateNotificationAsync(notification);
				return Ok();
			}
			catch (Exception ex)
			{
				return BadRequest($"Failed to create notification: {ex.Message}");
			}
		}

		/// <summary>
		/// Retrieves the list of notifications for the currently authenticated user.
		/// </summary>
		/// <remarks>This method requires the user to be authenticated. It fetches notifications based on the
		/// signed-in user's identity and returns appropriate status codes depending on the outcome of the
		/// operation.</remarks>
		/// <returns>An <see cref="ActionResult{T}"/> containing a list of <see cref="NotificationResponseDto"/> objects representing
		/// the user's notifications. Returns a 500 status code if the user is not signed in, a 400 status code if no
		/// notifications are found, or a 200 status code with the notifications if successful.</returns>
		[HttpGet, Authorize]
		public async Task<ActionResult<List<NotificationResponseDto>>> GetNotificationsAsync()
		{
			try
			{
				var user = await GetSignedInUserAsync();
				if (user == null) return StatusCode(500);

				var notifications = await notificationService.GetNotificationsAsync(user.Id);
				if (notifications == null || notifications.Count == 0) return BadRequest($"No notifications found for this user: {user.Id}");

				return Ok(notifications);
			}
			catch (Exception ex)
			{
				return BadRequest($"Failed to fetch notifications: {ex.Message}");
			}
		}

		/// <summary>
		/// Gets the number of unread notifications for the currently authenticated user.
		/// </summary>
		/// <remarks>This method requires the user to be authenticated. It retrieves the signed-in user's identifier
		/// and returns the count of unread notifications associated with that user.</remarks>
		/// <returns>An <see cref="ActionResult{T}"/> containing the number of unread notifications as an integer. Returns a status
		/// code 500 if the user is not signed in, or a BadRequest result if an error occurs while retrieving the count.</returns>
		[HttpGet("Count"), Authorize]
		public async Task<ActionResult<int>> GetNotificationCount()
		{
			try
			{
				var user = await GetSignedInUserAsync();
				if (user == null) return StatusCode(500);

				var count = await notificationService.GetNotificationCountAsync(user.Id);
				return Ok(count);
			}
			catch (Exception ex) 
			{
				return BadRequest($"Failed to fetch unread notification count: {ex.Message}");
			}
		}
	}
}
