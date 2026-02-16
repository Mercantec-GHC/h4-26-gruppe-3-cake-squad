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

		public ChatService(AppDbContext dbContext)
		{
			this.dbContext = dbContext;
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
	}
}