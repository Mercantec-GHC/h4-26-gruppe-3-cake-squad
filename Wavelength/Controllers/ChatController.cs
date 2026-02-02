using Commons.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Wavelength.Data;

namespace Wavelength.Controllers
{
	[ApiController]
	[Route("[controller]")]
	public class ChatController : ControllerBase
	{
		private readonly AppDbContext dbContext;
		public ChatController(AppDbContext dbContext)
		{
			this.dbContext = dbContext;
		}


		[HttpPost, Authorize]
		public async Task<ActionResult> CreateChatRoomAsync(CreateChatRoomDto dto)
		{
			var creator = await GetSignedInUserAsync();
			if (creator == null) return StatusCode(500);
			if (string.IsNullOrWhiteSpace(dto.RoomName)) return BadRequest("Room name can not be empty.");
			if (dto.ParticipantIds == null || dto.ParticipantIds.Count == 0) return BadRequest("At least one participant must be chosen.");
			foreach (var Id in dto.ParticipantIds)
			{
				if (!await dbContext.Users.AnyAsync(u => u.Id == Id)) return BadRequest("All id's on the list must exist on the database.");
			}
			// Make if statement to check if the creater has permission to create/see the participant!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

			// Creates the chat room.
			var chatRoom = new ChatRoom
			{
				Name = dto.RoomName
			};
			await dbContext.ChatRooms.AddAsync(chatRoom);

			// Creates a list of all the partisipants to be added on the chat room.
			var allParticipants = new List<Participant>();

			// Adds the creator to list of participants.
			allParticipants.Add(new Participant
			{
				UserId = creator.Id,
				ChatRoomId = chatRoom.Id
			});
			
			// Adds all the participantIds as a participant, to the list of participants.
			foreach (var id in dto.ParticipantIds)
			{
				allParticipants.Add(new Participant
				{
					UserId = id,
					ChatRoomId = chatRoom.Id
				});
			}

			await dbContext.Participants.AddRangeAsync(allParticipants);
			await dbContext.SaveChangesAsync();

			return Ok("Chat room was created.");
		}

		/// <summary>
		/// Asynchronously retrieves the currently signed-in user, if available.
		/// </summary>
		/// <returns>A <see cref="User"/> object representing the signed-in user, or <see langword="null"/> if no user is signed in or
		/// the user cannot be found.</returns>
		protected async Task<User?> GetSignedInUserAsync()
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (userId == null) return null;
			var user = await dbContext.Users.Where(u => u.Id == userId)
			 .Include(u => u.UserRoles)
			 .FirstOrDefaultAsync();
			if (user == null) return null;

			return user;
		}
	}
}