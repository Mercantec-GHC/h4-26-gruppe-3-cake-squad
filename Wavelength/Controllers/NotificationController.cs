using Commons.Models.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wavelength.Data;
using Commons.Enums;
using Microsoft.AspNetCore.Authorization;
using Wavelength.Services;

namespace Wavelength.Controllers
{
	[ApiController]
	[Route("[controller]")]
	public class NotificationController : BaseController
	{
		private readonly NotificationService notificationService;
		public NotificationController(AppDbContext context, NotificationService notificationService) : base(context) 
		{
			this.notificationService = notificationService;
		}

		[HttpPost, Authorize(Roles = "Admin")]
		public async Task<ActionResult> CreateNotificationAsync(AdminNotificationRequestDto request)
		{
			try 
			{
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
	}
}
