using Commons.Enums;
using Commons.Models.Database;
using Commons.Models.Dtos;
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

		/// <summary>
		/// Creates a new chat room with the specified name and participants.
		/// </summary>
		/// <remarks>The creator of the chat room is automatically added as a participant. All participant user IDs
		/// must exist in the database. The caller must be authorized to create chat rooms.</remarks>
		/// <param name="dto">An object containing the details required to create the chat room, including the room name and a list of
		/// participant user IDs. The room name cannot be null or empty, and at least one participant must be specified.</param>
		/// <returns>An ActionResult indicating the result of the operation. Returns 200 OK if the chat room is created successfully;
		/// otherwise, returns a 400 Bad Request if the input is invalid or a 500 Internal Server Error if the creator cannot
		/// be determined.</returns>
		[HttpPost, Authorize]
		public async Task<ActionResult> CreateChatRoomAsync(ChatRoomCreateDto dto)
		{
			// Validates the input dto.
			if (dto == null) return BadRequest("Request body can not be null.");
			if (string.IsNullOrWhiteSpace(dto.RoomName)) return BadRequest("RoomName can not be null or empty.");
			if (dto.ParticipantIds == null || dto.ParticipantIds.Count == 0) return BadRequest("At least one participant must be chosen.");
			foreach (var Id in dto.ParticipantIds)
			{
				if (!await dbContext.Users.AnyAsync(u => u.Id == Id)) return BadRequest("All id's on the list must exist on the database.");
			}

			// Gets the creator of the chat room.
			var creator = await GetSignedInUserAsync();
			if (creator == null) return StatusCode(500);
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
		/// Retrieves a list of all chat rooms available in the system.
		/// </summary>
		/// <remarks>This action requires the caller to have the 'Admin' role. The returned list includes basic
		/// information about each chat room and its participants.</remarks>
		/// <returns>An <see cref="ActionResult{T}"/> containing a list of <see cref="ChatRoomResponseDto"/> objects representing each chat
		/// room. Returns a 404 Not Found response if no chat rooms exist.</returns>
		[HttpGet, Authorize(Roles = "Admin")]
		public async Task<ActionResult<List<ChatRoomResponseDto>>> GetAllAsync()
		{
			// Retrieve all chat rooms
			List<ChatRoomResponseDto> chatRooms = await dbContext.ChatRooms
				.Select(cr => new ChatRoomResponseDto
				{
					Id = cr.Id,
					Name = cr.Name,
					Participants = cr.Participants
						.Select(p => p.UserId)
						.ToList()
				}).ToListAsync();
			if (chatRooms == null || chatRooms.Count == 0) return NotFound("No chat rooms found.");

			return Ok(chatRooms);
		}

		/// <summary>
		/// Retrieves the details of a chat room by its unique identifier.
		/// </summary>
		/// <remarks>Only users with the Admin role or users who are participants in the specified chat room are
		/// authorized to access this endpoint. If the user is not authorized or the chat room does not exist, an error
		/// response is returned.</remarks>
		/// <param name="chatRoomId">The unique identifier of the chat room to retrieve. Cannot be null or empty.</param>
		/// <returns>An <see cref="ActionResult{GetChatRoomDto}"/> containing the chat room details if found and accessible; otherwise,
		/// an appropriate error response such as 401 Unauthorized or 404 Not Found.</returns>
		[HttpGet("{chatRoomId}"), Authorize]
		public async Task<ActionResult<ChatRoomResponseDto>> GetChatRoomByIdAsync(string chatRoomId)
		{
			// Validate input
			if (string.IsNullOrWhiteSpace(chatRoomId)) return BadRequest("Chat room id can not be null or empty.");

			// Check permissions
			var user = await GetSignedInUserAsync();
			if (user == null) return StatusCode(500);
			if (user.Roles.Contains(RoleEnum.Admin) == false)
			{
				bool isParticipant = await dbContext.Participants
					.AnyAsync(p => p.ChatRoomId == chatRoomId && p.UserId == user.Id);
				if (!isParticipant) return Unauthorized("You do not have permission to access this chat room.");
			}

			// Retrieve chat room
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
			if (chatRoom == null) return NotFound("Chat room not found.");

			return Ok(chatRoom);
		}

		/// <summary>
		/// Updates the name of an existing chat room with the specified information.
		/// </summary>
		/// <remarks>Only users with the Admin role or participants of the specified chat room are authorized to
		/// perform this operation. The request must be authenticated.</remarks>
		/// <param name="dto">An object containing the updated chat room information. The <c>Id</c> property specifies the chat room to update,
		/// and the <c>Name</c> property provides the new name. Cannot be null. Both <c>Id</c> and <c>Name</c> must not be
		/// null or empty.</param>
		/// <returns>An <see cref="ActionResult"/> indicating the result of the operation. Returns <see cref="OkResult"/> if the update
		/// is successful; <see cref="BadRequestResult"/> if the input is invalid; <see cref="UnauthorizedResult"/> if the
		/// user does not have permission; <see cref="NotFoundResult"/> if the chat room does not exist; or <see
		/// cref="StatusCodeResult"/> with status 500 if the user cannot be determined.</returns>
		[HttpPut, Authorize]
		public async Task<ActionResult> UpdateChatRoomAsync([FromBody] ChatRoomUpdateDto dto)
		{
			// Validate input
			if (dto == null) return BadRequest("Request body can not be null.");
			if (string.IsNullOrWhiteSpace(dto.Id)) return BadRequest("Chat room id can not be null or empty.");
			if (string.IsNullOrWhiteSpace(dto.Name)) return BadRequest("Chat room name can not be null or empty.");

			// Check permissions
			var user = await GetSignedInUserAsync();
			if (user == null) return StatusCode(500);
			if (user.Roles.Contains(RoleEnum.Admin) == false)
			{
				bool isParticipant = await dbContext.Participants
					.AnyAsync(p => p.ChatRoomId == dto.Id && p.UserId == user.Id);
				if (!isParticipant) return Unauthorized("You do not have permission to access this chat room.");
			}

			// Update chat room
			var chatRoom = await dbContext.ChatRooms
				.FirstOrDefaultAsync(cr => cr.Id == dto.Id);
			if (chatRoom == null) return NotFound("Chat room not found.");

			chatRoom.Name = dto.Name;
			await dbContext.SaveChangesAsync();

			return Ok("Chat room updated successfully.");
		}

		/// <summary>
		/// Removes the signed-in user from the specified chat room.
		/// </summary>
		/// <param name="chatRoomId">The unique identifier of the chat room to leave. Cannot be null or empty.</param>
		/// <returns>An <see cref="ActionResult"/> indicating the result of the operation. Returns 200 OK if the user successfully
		/// leaves the chat room; 400 Bad Request if <paramref name="chatRoomId"/> is null or empty; 404 Not Found if the user
		/// is not a participant of the specified chat room; or 500 Internal Server Error if the user context cannot be
		/// determined.</returns>
		[HttpDelete("leave/{chatRoomId}"), Authorize]
		public async Task<ActionResult> LeaveChatRoomAsync(string chatRoomId)
		{
			// Validate input
			if (string.IsNullOrWhiteSpace(chatRoomId)) return BadRequest("Chat room id can not be null or empty.");

			// Check if the user is a participant
			var user = await GetSignedInUserAsync();
			if (user == null) return StatusCode(500);

			var participant = await dbContext.Participants
				.FirstOrDefaultAsync(p => p.ChatRoomId == chatRoomId && p.UserId == user.Id);
			if (participant == null) return NotFound("You are not a participant of this chat room.");

			dbContext.Participants.Remove(participant);
			await dbContext.SaveChangesAsync();

			return Ok("User has left the chat room.");
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