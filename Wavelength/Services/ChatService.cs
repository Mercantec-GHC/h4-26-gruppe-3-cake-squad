using Commons.Enums;
using Commons.Models.Database;
using Commons.Models.Dtos;
using Microsoft.EntityFrameworkCore;
using Wavelength.Data;

namespace Wavelength.Services
{
	/// <summary>
	/// Provides methods for managing chat rooms and messages, including creating, retrieving, updating, and deleting chat
	/// rooms and messages.
	/// </summary>
	/// <remarks>The ChatService enforces user permissions and visibility when performing chat-related operations.
	/// It utilizes a database context for data access, an encryption service to secure chat messages, and a notification
	/// service to alert users of chat activity. All operations are asynchronous and require appropriate user
	/// authorization. This service is intended to be used as the primary interface for chat functionality within the
	/// application.</remarks>
	public class ChatService
	{
		private readonly AppDbContext dbContext;
		private readonly AesEncryptionService aesEncryption;
		private readonly NotificationService notificationService;

		/// <summary>
		/// Initializes a new instance of the ChatService class using the specified database context, encryption service, and
		/// notification service.
		/// </summary>
		/// <param name="dbContext">The database context used to access and manage chat-related data.</param>
		/// <param name="aesEncryption">The AES encryption service used to encrypt and decrypt sensitive chat messages.</param>
		/// <param name="notificationService">The notification service used to send alerts and updates related to chat activities.</param>
		public ChatService(AppDbContext dbContext, AesEncryptionService aesEncryption, NotificationService notificationService)
		{
			this.dbContext = dbContext;
			this.aesEncryption = aesEncryption;
			this.notificationService = notificationService;
		}

		/// <summary>
		/// Creates a new chat room with the specified participants, ensuring that all participant IDs are valid and visible
		/// to the creator.
		/// </summary>
		/// <remarks>The creator is automatically excluded from the participant list, and only users visible to the
		/// creator are included as participants. All participant IDs must be unique.</remarks>
		/// <param name="dto">A data transfer object containing the details required to create the chat room, including the room name and a list
		/// of participant user IDs.</param>
		/// <param name="creator">The user who is creating the chat room. Used to determine participant visibility and to exclude the creator from
		/// the participant list.</param>
		/// <returns>A task that represents the asynchronous operation of creating the chat room and its participants.</returns>
		/// <exception cref="ArgumentException">Thrown if one or more participant IDs in <paramref name="dto"/> are invalid or do not exist in the database.</exception>
		public async Task CreateChatRoomAsync(ChatRoomCreateDto dto, User creator)
		{
			// Removes the creator from the participant list, if present, and ensures all participant IDs are unique.
			dto.ParticipantIds = dto.ParticipantIds
				.Where(p => p != creator.Id)
				.Distinct()
				.ToList();

			// Validates that all participant IDs exist in the database.
			if (await dbContext.Users
				.Where(u => dto.ParticipantIds.Contains(u.Id))
				.CountAsync() != dto.ParticipantIds.Count()
			) throw new ArgumentException("One or more participant IDs are invalid.", nameof(dto.ParticipantIds));

			// Filters the participant IDs to only include users who are visible to the creator based on user visibilities.
			dto.ParticipantIds = creator.UserVisibilities
				.Where(uv => dto.ParticipantIds.Contains(uv.TargetUserId) &&
					uv.Visibility == UserVisibilityEnum.Visible)
				.Select(uv => uv.TargetUserId)
				.ToList();

			var chatRoom = new ChatRoom
			{
				Name = dto.RoomName
			};
			await dbContext.ChatRooms.AddAsync(chatRoom);

			var allParticipants = new List<Participant>();

			allParticipants.Add(new Participant
			{
				UserId = creator.Id,
				ChatRoomId = chatRoom.Id,
			});

			foreach (var id in dto.ParticipantIds)
			{
				allParticipants.Add(new Participant
				{
					UserId = id,
					ChatRoomId = chatRoom.Id,
				});
			}

			await dbContext.Participants.AddRangeAsync(allParticipants);
			await dbContext.SaveChangesAsync();
		}

		/// <summary>
		/// Asynchronously retrieves all chat rooms along with their participants.
		/// </summary>
		/// <remarks>This method fetches chat rooms from the database and maps them to response DTOs. Ensure that the
		/// database context is properly initialized before calling this method.</remarks>
		/// <returns>A list of <see cref="ChatRoomResponseDto"/> objects representing the chat rooms. The list will be empty if no chat
		/// rooms are found.</returns>
		/// <exception cref="ArgumentException">Thrown if no chat rooms are found.</exception>
		public async Task<List<ChatRoomResponseDto>> GetAllAsync()
		{
			var chatRooms = await dbContext.ChatRooms
				.Select(cr => new ChatRoomResponseDto
				{
					Id = cr.Id,
					Name = cr.Name,
					Participants = cr.Participants
						.Select(p => p.UserId)
						.ToList()
				}).ToListAsync();
			if (chatRooms == null || chatRooms.Count == 0) throw new ArgumentException("No chat rooms found.", nameof(chatRooms));

			return chatRooms;
		}

		/// <summary>
		/// Asynchronously retrieves the details of a chat room by its unique identifier, ensuring that the specified user has
		/// permission to access the chat room.
		/// </summary>
		/// <param name="chatRoomId">The unique identifier of the chat room to retrieve. Cannot be null.</param>
		/// <param name="user">The user requesting access to the chat room. The user must be an administrator or a participant in the chat room.</param>
		/// <returns>A task that represents the asynchronous operation. The task result contains a ChatRoomResponseDto with the chat
		/// room's identifier, name, and a list of participant user IDs.</returns>
		/// <exception cref="ArgumentException">Thrown if the user does not have permission to access the specified chat room.</exception>
		/// <exception cref="ArgumentNullException">Thrown if no chat room with the specified identifier is found.</exception>
		public async Task<ChatRoomResponseDto> GetChatRoomByIdAsync(string chatRoomId, User user)
		{
			if (user.Roles.Contains(RoleEnum.Admin) == false)
			{
				bool isParticipant = await dbContext.Participants
					.AnyAsync(p => p.ChatRoomId == chatRoomId && p.UserId == user.Id);
				if (!isParticipant) throw new ArgumentException("You do not have permission to access this chat room.", nameof(user.FirstName));
			}

			var chatRoom = await dbContext.ChatRooms
				.Where(cr => cr.Id == chatRoomId)
				.Select(cr => new ChatRoomResponseDto
				{
					Id = cr.Id,
					Name = cr.Name,
					Participants = cr.Participants
						.Select(p => p.UserId)
						.ToList()
				}).FirstOrDefaultAsync();
			if (chatRoom == null) throw new ArgumentNullException(nameof(chatRoomId), "No chat room with that id was found.");

			return chatRoom;
		}

		/// <summary>
		/// Updates the details of an existing chat room with the specified information.
		/// </summary>
		/// <remarks>The user must be either an administrator or a participant in the chat room to perform the update.
		/// Only the chat room's name is updated.</remarks>
		/// <param name="dto">A data transfer object containing the updated chat room information, including the chat room identifier and the
		/// new name.</param>
		/// <param name="user">The user requesting the update. The user must be an administrator or a participant in the chat room.</param>
		/// <returns>A task that represents the asynchronous operation of updating the chat room.</returns>
		/// <exception cref="ArgumentException">Thrown if the user does not have permission to access the specified chat room.</exception>
		/// <exception cref="ArgumentNullException">Thrown if no chat room exists with the specified identifier.</exception>
		public async Task UpdateChatRoomAsync(ChatRoomUpdateDto dto, User user)
		{
			if (user.Roles.Contains(RoleEnum.Admin) == false)
			{
				bool isParticipant = await dbContext.Participants
					.AnyAsync(p => p.ChatRoomId == dto.Id && p.UserId == user.Id);
				if (!isParticipant) throw new ArgumentException("You do not have permission to access this chat room.", nameof(user.FirstName));
			}

			var chatRoom = await dbContext.ChatRooms
				.FirstOrDefaultAsync(cr => cr.Id == dto.Id);
			if (chatRoom == null) throw new ArgumentNullException(nameof(chatRoom.Id), "No chat room found matching that id.");

			chatRoom.Name = dto.Name;
			await dbContext.SaveChangesAsync();
		}

		/// <summary>
		/// Removes the specified user from the chat room identified by the given chat room ID. If the chat room has no
		/// remaining participants after the user leaves, the chat room is deleted.
		/// </summary>
		/// <remarks>If the chat room becomes empty after the user is removed, it is also deleted from the database.
		/// Ensure that the user is a participant of the chat room before calling this method.</remarks>
		/// <param name="chatRoomId">The unique identifier of the chat room from which the user will be removed. Cannot be null.</param>
		/// <param name="user">The user to remove from the chat room. Must be a current participant of the specified chat room. Cannot be null.</param>
		/// <returns>A task that represents the asynchronous operation of removing the user from the chat room.</returns>
		/// <exception cref="ArgumentNullException">Thrown if the chat room with the specified ID does not exist, or if the user is not a participant of the chat
		/// room.</exception>
		public async Task LeaveChatRoomAsync(string chatRoomId, User user)
		{
			var chatRoom = await dbContext.ChatRooms
				.Include(cr => cr.Participants)
				.FirstOrDefaultAsync(cr => cr.Id == chatRoomId);
			if (chatRoom == null) throw new ArgumentNullException(nameof(chatRoom.Id), "No chat room with that id.");

			var participant = chatRoom.Participants
				.FirstOrDefault(p => p.UserId == user.Id);
			if (participant == null) throw new ArgumentNullException(nameof(participant.Id), "The user is not a participant of this chat room.");
			chatRoom.Participants.Remove(participant);

			if (!chatRoom.Participants.Any())
			{
				dbContext.ChatRooms.Remove(chatRoom);
			}

			await dbContext.SaveChangesAsync();
		}

		/// <summary>
		/// Removes the specified participants from a chat room as an administrative action.
		/// </summary>
		/// <remarks>All participant identifiers must correspond to existing users and must be members of the
		/// specified chat room. If the chat room has no remaining participants after removal, the chat room is also
		/// deleted.</remarks>
		/// <param name="dto">A data transfer object containing the identifiers of participants to remove and the identifier of the chat room.</param>
		/// <returns>A task that represents the asynchronous remove operation.</returns>
		/// <exception cref="ArgumentException">Thrown when one or more participant identifiers in the provided list do not exist in the database.</exception>
		/// <exception cref="ArgumentNullException">Thrown when the specified chat room is not found, or when none of the specified participants are members of the
		/// chat room.</exception>
		public async Task AdminRemoveParticipantsAsync(ParticipantRemoveDtos dto)
		{
			if (await dbContext.Users
				.Where(u => dto.ParticipantIds.Contains(u.Id))
				.CountAsync() != dto.ParticipantIds.Count()
			) throw new ArgumentException("All id's on the list must exist on the database.", nameof(dto.ParticipantIds));

			var chatRoom = await dbContext.ChatRooms
				.Include(cr => cr.Participants)
				.FirstOrDefaultAsync(cr => cr.Id == dto.ChatRoomId);
			if (chatRoom == null) throw new ArgumentNullException(nameof(chatRoom.Id), "No chat room found with that id.");

			var participants = chatRoom.Participants
				.Where(p => dto.ParticipantIds.Contains(p.UserId))
				.ToList();
			if (participants == null) throw new ArgumentNullException(nameof(participants), "No of the participants are a part of this chat room.");

			foreach (var participant in participants)
			{
				chatRoom.Participants.Remove(participant);
			}

			if (!chatRoom.Participants.Any())
			{
				dbContext.ChatRooms.Remove(chatRoom);
			}

			await dbContext.SaveChangesAsync();
		}

		/// <summary>
		/// Asynchronously sends a chat message to the specified chat room and notifies all participants of the new message.
		/// </summary>
		/// <param name="dto">A data transfer object containing the details of the chat message to send, including the chat room identifier and
		/// message content.</param>
		/// <param name="sender">The user who is sending the message. The sender must be a participant in the specified chat room.</param>
		/// <returns>A task that represents the asynchronous operation of sending the message and creating the notification.</returns>
		/// <exception cref="ArgumentNullException">Thrown if no chat room exists for the specified chat room identifier in <paramref name="dto"/>.</exception>
		/// <exception cref="ArgumentException">Thrown if the sender is not a participant in the specified chat room.</exception>
		/// <exception cref="Exception">Thrown if the notification for the new chat message fails to be created.</exception>
		public async Task SendMessageAsync(ChatMessageCreateDto dto, User sender)
		{
			var chatRoom = await dbContext.ChatRooms
				.Include(cr => cr.Participants)
				.Include(cr => cr.ChatMessages)
				.FirstOrDefaultAsync(cr => cr.Id == dto.ChatRoomId);
			if (chatRoom == null) throw new ArgumentNullException(nameof(chatRoom.Id), "No chat room was found.");
			if (!chatRoom.Participants.Any(p => p.UserId == sender.Id)) throw new ArgumentException("No other partisipants in this chat.");

			var message = new ChatMessage
			{
				ChatRoomId = dto.ChatRoomId,
				SenderId = sender.Id,
				MessageContent = aesEncryption.Encrypt(dto.MessageContent)
			};

			await dbContext.ChatMessages.AddAsync(message);
			await dbContext.SaveChangesAsync();

			var notificationRequest = new MessageNotificationRequestDto
			{
				SenderId = sender.Id,
				ChatRoomId = dto.ChatRoomId,
				ObjectId = message.Id.ToString(),
				Content = $"New message in chat room '{chatRoom.Name}' from {sender.FirstName} {sender.LastName}."
			};

			try
			{
				await notificationService.CreateMessageNotificationAsync(notificationRequest);
			}
			catch (Exception ex)
			{
				throw new Exception("Failed to create notification for new chat message.", ex);
			}
		}

		/// <summary>
		/// Retrieves all chat messages from the specified chat room, supporting cursor-based pagination.
		/// </summary>
		/// <remarks>If the user is not an administrator, their participation in the chat room is verified before
		/// messages are retrieved. Pagination is implemented using a cursor, and the response includes information to
		/// facilitate subsequent paginated requests.</remarks>
		/// <param name="dto">A request object that specifies the chat room identifier and an optional cursor for paginating results.</param>
		/// <param name="user">The user requesting the messages. The user's authorization to access the chat room is validated.</param>
		/// <returns>A ChatMessageResponseDto containing a collection of message objects, the next pagination cursor, and a flag
		/// indicating whether additional messages are available.</returns>
		/// <exception cref="ArgumentException">Thrown if the user is not authorized to access the specified chat room.</exception>
		public async Task<ChatMessageResponseDto> GetAllChatRoomMessagesAsync(ChatMessageRequestDto dto, User user)
		{
			if (!user.Roles.Contains(RoleEnum.Admin))
			{
				if (!await dbContext.Participants
					.AnyAsync(p => p.ChatRoomId == dto.ChatRoomId &&
						p.UserId == user.Id)
				) throw new ArgumentException("User is unauthorized.", nameof(user));
			}

			var query = dbContext.ChatMessages.Where(cm => cm.ChatRoomId == dto.ChatRoomId);

			// Apply cursor-based pagination.
			if (dto.Cursor.HasValue) query = query.Where(cm => cm.CreatedAt < dto.Cursor.Value);

			var pageSize = 10;

			var messages = await query
				.OrderByDescending(cm => cm.CreatedAt)
				.Take(pageSize + 1)
				.Select(cm => new MessageObjectDto
				{
					Id = cm.Id,
					CreatedAt = cm.CreatedAt,
					MessageContent = aesEncryption.Decrypt(cm.MessageContent),
					Sender = new UserMessageDto
					{
						Id = cm.Sender.Id,
						FirstName = cm.Sender.FirstName,
						LastName = cm.Sender.LastName
					}
				})
				.ToListAsync();

			bool hasMore = messages.Count > pageSize;

			if (hasMore) messages.RemoveAt(messages.Count - 1);

			return new ChatMessageResponseDto
			{
				MessageObjects = messages,
				NextCursor = messages.LastOrDefault()?.CreatedAt,
				HasMore = hasMore
			};
		}

		/// <summary>
		/// Removes a chat message from the chat room identified by the specified message ID.
		/// </summary>
		/// <remarks>Only users with the Admin role or the original sender of the message can remove it. The method
		/// checks the user's roles and their participation in the chat room before proceeding with the removal.</remarks>
		/// <param name="messageId">The unique identifier of the chat message to be removed.</param>
		/// <param name="user">The user attempting to remove the chat message. The user must have appropriate permissions to perform this action.</param>
		/// <returns></returns>
		/// <exception cref="ArgumentNullException">Thrown if no chat message is found with the specified message ID.</exception>
		/// <exception cref="ArgumentException">Thrown if the user is not authorized to remove the message or if the user is not the sender of the message and is
		/// not an admin.</exception>
		public async Task RemoveChatRoomMessageAsync(int messageId, User user)
		{
			var chatMessage = await dbContext.ChatMessages.FirstOrDefaultAsync(cm => cm.Id == messageId);
			if (chatMessage == null) throw new ArgumentNullException(nameof(chatMessage), "No chat message was found with that id.");

			if (!user.Roles.Contains(RoleEnum.Admin))
			{
				if (!await dbContext.Participants
					.AnyAsync(p => p.ChatRoomId == chatMessage.ChatRoomId &&
						p.UserId == user.Id)
				) throw new ArgumentException("User is not authorized.");

				if (chatMessage.SenderId != user.Id) throw new ArgumentException("User is ");
			}

			dbContext.ChatMessages.Remove(chatMessage);
			await dbContext.SaveChangesAsync();
		}
	}
}