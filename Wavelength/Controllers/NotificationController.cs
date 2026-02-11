using Commons.Models.Database;
using Commons.Models.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wavelength.Data;
using Commons.Enums;

namespace Wavelength.Controllers
{
	[ApiController]
	[Route("[controller]")]
	public class NotificationController : BaseController
	{
		public NotificationController(AppDbContext context) : base(context) { }

		[HttpPost]
		public async Task<ActionResult> CreateNotificationAsync(NotificationRequestDto request)
		{
			if (request == null) return BadRequest("Request body can not be empty.");
			if (string.IsNullOrWhiteSpace(request.SenderId)) return BadRequest("Sender id can not be empty.");
			if (string.IsNullOrWhiteSpace(request.TargetId)) return BadRequest("Target id can not be empty.");
			if (string.IsNullOrWhiteSpace(request.Content)) return BadRequest("Content can not be empty.");

			var sender = await GetSignedInUserAsync(q => q.Include(u => u.UserVisibilities));
			if (sender == null) return StatusCode(500);

			if (!await DbContext.Users.AnyAsync(u => u.Id == request.TargetId)) return BadRequest("Target id does not exist.");

			var notification = new Notification
			{
				SenderId = sender.Id,
				TargetId = request.TargetId,
				ObjectId = null,
				Content = request.Content,
				Type = NotificationTypeEnum.System
			};

			await DbContext.Notifications.AddAsync(notification);
			await DbContext.SaveChangesAsync();

			return Ok();
		}
	}
}
