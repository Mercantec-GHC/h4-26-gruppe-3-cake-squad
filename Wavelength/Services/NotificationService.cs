using Commons.Enums;
using Commons.Models.Database;
using Commons.Models.Dtos;
using Microsoft.EntityFrameworkCore;
using Wavelength.Data;

namespace Wavelength.Services
{
	/// <summary>
	/// Provides methods for creating and retrieving notifications for users within the application.
	/// </summary>
	/// <remarks>This service interacts with the application's database context to manage notifications. It includes
	/// methods for creating notifications for both general alerts and message notifications, as well as retrieving
	/// notifications for a specific user. Ensure that the provided user IDs and content are valid to avoid exceptions
	/// during operations.</remarks>
	public class NotificationService
	{
		private readonly AppDbContext dbContext;

		/// <summary>
		/// Initializes a new instance of the NotificationService class using the specified database context.
		/// </summary>
		/// <param name="dbContext">The database context to be used for accessing and managing notification-related data.</param>
		public NotificationService(AppDbContext dbContext) 
		{
			this.dbContext = dbContext;
		}

		/// <summary>
		/// Creates and stores a system notification for each specified target user based on the provided request.
		/// </summary>
		/// <remarks>This method is intended for administrative use to send system notifications to multiple users.
		/// All target user IDs must correspond to existing users in the database.</remarks>
		/// <param name="request">An object containing the details of the notification to create, including the sender's user ID, a collection of
		/// target user IDs, and the notification content. Cannot be null, and all properties must be populated with valid
		/// values.</param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="request"/> is null.</exception>
		/// <exception cref="ArgumentException">Thrown if <paramref name="request.SenderId"/> is null or empty, if <paramref name="request.TargetIds"/> is null or
		/// empty, if <paramref name="request.Content"/> is null or empty, or if any target user ID does not exist in the
		/// database.</exception>
		public async Task AdminCreateNotificationAsync(NotificationRequestDto request)
		{
			if (await dbContext.Users
				.Where(u => request.TargetIds.Contains(u.Id))
				.CountAsync() != request.TargetIds.Count())
				throw new InvalidOperationException("Not all users on the list exicts.");

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

		/// <summary>
		/// Creates and stores message notifications for all participants in the specified chat room, excluding the sender.
		/// </summary>
		/// <remarks>Notifications are only created for valid participants in the chat room, and the sender is always
		/// excluded from receiving a notification. All required fields must be provided for the operation to
		/// succeed.</remarks>
		/// <param name="request">An object containing the details of the message notification, including the sender ID, chat room ID, and message
		/// content. Cannot be null.</param>
		/// <returns>A task that represents the asynchronous operation of creating message notifications for the chat room
		/// participants.</returns>
		/// <exception cref="ArgumentNullException">Thrown when the <paramref name="request"/> is null or when the specified chat room ID does not exist.</exception>
		/// <exception cref="ArgumentException">Thrown when the sender ID, chat room ID, or content is null or empty; when the sender ID does not exist; or when
		/// no valid notification targets are found in the chat room.</exception>
		public async Task CreateMessageNotificationAsync(MessageNotificationRequestDto request)
		{
			if (!await dbContext.Users.AnyAsync(u => u.Id == request.SenderId)) throw new KeyNotFoundException("Sender id does not exist.");

			var chatRoom = await dbContext.ChatRooms
				.Include(cr => cr.Participants)
				.FirstOrDefaultAsync(cr => cr.Id == request.ChatRoomId);
			if (chatRoom == null) throw new KeyNotFoundException("Chat room id does not exist.");

			var targets = chatRoom.Participants
				.Where(p => p.UserId != request.SenderId)
				.Select(p => p.UserId)
				.ToList();
			if (targets == null || targets.Count == 0) throw new InvalidOperationException("No valid target found in the chat room.");

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

		/// <summary>
		/// Asynchronously retrieves a list of notifications for the specified user, ordered by creation date in descending
		/// order.
		/// </summary>
		/// <param name="userId">The unique identifier of the user whose notifications are to be retrieved. Cannot be null, empty, or consist only
		/// of white-space characters.</param>
		/// <returns>A task that represents the asynchronous operation. The task result contains a list of <see
		/// cref="NotificationResponseDto"/> objects representing the user's notifications.</returns>
		/// <exception cref="ArgumentException">Thrown if <paramref name="userId"/> is null, empty, consists only of white-space characters, or if no
		/// notifications are found for the specified user.</exception>
		public async Task<List<NotificationResponseDto>> GetNotificationsAsync(string userId)
		{
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
			if (notifications == null || notifications.Count == 0) throw new KeyNotFoundException("No notifications found for the user.");
			
			return notifications;
		}

		/// <summary>
		/// Asynchronously retrieves the total number of notifications associated with the specified user.
		/// </summary>
		/// <remarks>This method queries the underlying data store for notifications targeting the specified user. If
		/// no notifications exist for the user, an exception is thrown rather than returning zero.</remarks>
		/// <param name="userId">The unique identifier of the user whose notifications are to be counted. Cannot be null, empty, or consist only of
		/// white-space characters.</param>
		/// <returns>A task that represents the asynchronous operation. The task result contains the number of notifications for the
		/// specified user.</returns>
		/// <exception cref="ArgumentException">Thrown if <paramref name="userId"/> is null, empty, or consists only of white-space characters, or if no
		/// notifications are found for the specified user.</exception>
		public async Task<int> GetNotificationCountAsync(string userId)
		{
			var count = await dbContext.Notifications
				.Where(n => n.TargetId == userId)
				.CountAsync();
			if (count == 0) throw new KeyNotFoundException("No notifications found for the user.");

			return count;
		}
	}
}