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

		public async Task AdminCreateNotificationAsync(NotificationRequestDto request)
		{
			if (request == null) throw new ArgumentNullException(nameof(request), "Request body can not be empty.");
			if (string.IsNullOrWhiteSpace(request.SenderId)) throw new ArgumentException("Sender id can not be empty.", nameof(request.SenderId));
			if (request.TargetIds == null || 
				request.TargetIds.Count == 0)
			throw new ArgumentException("Target id can not be empty.", nameof(request.TargetIds));
			if (string.IsNullOrWhiteSpace(request.Content)) throw new ArgumentException("Content can not be empty.", nameof(request.Content));

			if (await dbContext.Users
				.Where(u => request.TargetIds.Contains(u.Id))
				.CountAsync() != request.TargetIds.Count())
				throw new ArgumentException("Not all users on the list exicts.", nameof(request.TargetIds));

			List<Notification> notifications = new();

			foreach (var targetId in request.TargetIds)
			{
				var notification = new Notification
				{
					SenderId = request.SenderId,
					TargetId = targetId,
					Content = request.Content,
					Type = NotificationTypeEnum.System
				};
				notifications.Add(notification);
			}

			await dbContext.Notifications.AddRangeAsync(notifications);
			await dbContext.SaveChangesAsync();
		}

		public async Task CreateMessageNotificationAsync(MessageNotificationRequestDto request)
		{
			if (request == null) throw new ArgumentNullException(nameof(request), "Request body can not be empty.");
			if (string.IsNullOrWhiteSpace(request.SenderId)) throw new ArgumentException("Sender id can not be empty.", nameof(request.SenderId));
			if (string.IsNullOrWhiteSpace(request.ChatRoomId)) throw new ArgumentException("Target id can not be empty.", nameof(request.ChatRoomId));
			if (string.IsNullOrWhiteSpace(request.Content)) throw new ArgumentException("Content can not be empty.", nameof(request.Content));

			if (!await dbContext.Users.AnyAsync(u => u.Id == request.SenderId)) throw new ArgumentException("Sender id does not exist.");

			var chatRoom = await dbContext.ChatRooms
				.Include(cr => cr.Participants)
				.FirstOrDefaultAsync(cr => cr.Id == request.ChatRoomId);
			if (chatRoom == null) throw new ArgumentNullException(nameof(request.ChatRoomId), "Chat room id does not exist.");

			var targets = chatRoom.Participants
				.Where(p => p.UserId != request.SenderId)
				.Select(p => p.UserId)
				.ToList();
			if (targets == null || targets.Count == 0) throw new ArgumentException("No valid target found in the chat room.", nameof(request.ChatRoomId));

			List<Notification> notifications = new();

			foreach (var targetId in targets)
			{
				var notification = new Notification
				{
					SenderId = request.SenderId,
					TargetId = targetId,
					ObjectId = request.ChatRoomId,
					Content = request.Content,
					Type = NotificationTypeEnum.Message
				};
				notifications.Add(notification);
			}

			await dbContext.Notifications.AddRangeAsync(notifications);
			await dbContext.SaveChangesAsync();
		}

		public async Task<List<NotificationResponseDto>> GetNotificationsAsync(string userId)
		{
			if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("User id can not be empty.", nameof(userId));

			var notifications = await dbContext.Notifications
				.Where(n => n.TargetId == userId)
				.OrderByDescending(n => n.CreatedAt)
				.Select(n => new NotificationResponseDto
				{
					Id = n.Id,
					SenderId = n.SenderId,
					ObjectId = n.ObjectId ?? null,
					Content = n.Content,
					Type = n.Type,
					CreatedAt = n.CreatedAt
				})
				.ToListAsync();
			if (notifications == null || notifications.Count == 0) throw new ArgumentException("No notifications found for the user.", nameof(userId));
			
			return notifications;
		}

		public async Task<int> GetNotificationCountAsync(string userId)
		{
			if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentException("User id can not be empty.", nameof(userId));

			var count = await dbContext.Notifications
				.Where(n => n.TargetId == userId)
				.CountAsync();
			if (count == 0) throw new ArgumentException("No notifications found for the user.", nameof(userId));

			return count;
		}
	}
}