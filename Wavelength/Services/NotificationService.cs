using Commons.Enums;
using Commons.Models.Database;
using Commons.Models.Dtos;
using Microsoft.EntityFrameworkCore;
using Wavelength.Data;

namespace Wavelength.Services
{
	public class NotificationService
	{
		private readonly AppDbContext dbContext;
		public NotificationService(AppDbContext dbContext) 
		{
			this.dbContext = dbContext;
		}

		public async Task CreateMessageNotificationAsync(NotificationRequestDto request)
		{
			if (request == null) throw new ArgumentNullException(nameof(request), "Request body can not be empty.");
			if (string.IsNullOrWhiteSpace(request.SenderId)) throw new ArgumentException("Sender id can not be empty.", nameof(request.SenderId));
			if (string.IsNullOrWhiteSpace(request.TargetId)) throw new ArgumentException("Target id can not be empty.", nameof(request.TargetId));
			if (string.IsNullOrWhiteSpace(request.Content)) throw new ArgumentException("Content can not be empty.", nameof(request.Content));

			if (!await dbContext.Users.AnyAsync(u => u.Id == request.SenderId && u.Id == request.TargetId)) throw new ArgumentException("Sender id or target id does not exist.");

			var notification = new Notification
			{
				SenderId = request.SenderId,
				TargetId = request.TargetId,
				ObjectId = request.ObjectId,
				Content = request.Content,
				Type = NotificationTypeEnum.Message
			};

			await dbContext.Notifications.AddAsync(notification);
			await dbContext.SaveChangesAsync();
		}
	}
}