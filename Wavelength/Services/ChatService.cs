using Commons.Enums;
using Commons.Models.Database;
using Commons.Models.Dtos;
using Microsoft.EntityFrameworkCore;
using Wavelength.Data;

namespace Wavelength.Services
{
	public class ChatService
	{
		private readonly AppDbContext dbContext;
		private readonly AesEncryptionService aesEncryption;
		private readonly NotificationService notificationService;

		public ChatService(AppDbContext dbContext, AesEncryptionService aesEncryption, NotificationService notificationService)
		{
			this.dbContext = dbContext;
			this.aesEncryption = aesEncryption;
			this.notificationService = notificationService;
		}

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
	}
}