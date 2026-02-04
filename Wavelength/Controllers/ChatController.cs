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
	public class ChatController : BaseController
	{
		public ChatController(AppDbContext dbContext) : base(dbContext) { }

		#region Chat Room Management

		/// <summary>
		/// Creates a new chat room with the specified name and participants.
		/// </summary>
		/// <remarks>The creator of the chat room is automatically added as a participant. Only users who have a
		/// user-visible question score with the creator are included as participants. All participant user IDs must exist in
		/// the database. This action requires authentication.</remarks>
		/// <param name="dto">An object containing the details required to create the chat room, including the room name and a list of
		/// participant user IDs. The room name cannot be null or empty. The participant list must contain at least one user
		/// ID.</param>
		/// <returns>An HTTP result indicating the outcome of the operation. Returns 200 OK if the chat room is created successfully;
		/// otherwise, returns a 400 Bad Request or 500 Internal Server Error with an appropriate error message.</returns>
		[HttpPost, Authorize]
		public async Task<ActionResult> CreateChatRoomAsync(ChatRoomCreateDto dto)
		{
			// Validate input.
			if (dto == null) return BadRequest("Request body can not be null.");
			if (string.IsNullOrWhiteSpace(dto.RoomName)) return BadRequest("RoomName can not be null or empty.");
			if (dto.ParticipantIds == null || dto.ParticipantIds.Count == 0) return BadRequest("At least one participant must be chosen.");

			var creator = await GetSignedInUserAsync();
			if (creator == null) return StatusCode(500);

			// Removes the creator from the participant list, if present, and ensures all participant IDs are unique.
			dto.ParticipantIds = dto.ParticipantIds
				.Where(p => p != creator.Id)
				.Distinct()
				.ToList();

			// Validates that all participant IDs exist in the database.
			if (await DbContext.Users
				.Where(u => dto.ParticipantIds.Contains(u.Id))
				.CountAsync() != dto.ParticipantIds.Count()
			) return BadRequest("All id's on the list must exist on the database.");

			// Filters the participant IDs to only include users who have a user-visible question score with the creator.
			dto.ParticipantIds = await DbContext.QuestionScores
				.Where(qs => qs.PlayerId == creator.Id &&
					dto.ParticipantIds.Contains(qs.QuizOwnerId) &&
					qs.IsUserVisible)
				.Select(qs => qs.QuizOwnerId)
				.ToListAsync();
						
			var chatRoom = new ChatRoom
			{
				Name = dto.RoomName
			};
			await DbContext.ChatRooms.AddAsync(chatRoom);

			// Creates a list of all the partisipants to be added on the chat room.
			var allParticipants = new List<Participant>();

			allParticipants.Add(new Participant
			{
				UserId = creator.Id,
				ChatRoomId = chatRoom.Id
			});
			
			foreach (var id in dto.ParticipantIds)
			{
				allParticipants.Add(new Participant
				{
					UserId = id,
					ChatRoomId = chatRoom.Id
				});
			}

			await DbContext.Participants.AddRangeAsync(allParticipants);
			await DbContext.SaveChangesAsync();

			return Ok("Chat room was created.");
		}

		/// <summary>
		/// Retrieves a list of all chat rooms.
		/// </summary>
		/// <remarks>This endpoint is restricted to users with the Admin role. The returned list includes all chat
		/// rooms and their associated participant user IDs.</remarks>
		/// <returns>An <see cref="ActionResult{T}"/> containing a list of <see cref="ChatRoomResponseDto"/> objects representing all
		/// chat rooms. Returns a 404 Not Found response if no chat rooms exist.</returns>
		[HttpGet, Authorize(Roles = "Admin")]
		public async Task<ActionResult<List<ChatRoomResponseDto>>> GetAllAsync()
		{
			// Retrieve all chat rooms
			List<ChatRoomResponseDto> chatRooms = await DbContext.ChatRooms
				.Select(cr => new ChatRoomResponseDto
				{
					Id = cr.Id,
					Name = cr.Name,
					Participants = cr.Participants
						.Select(p => p.UserId)
						.ToList()
				}).ToListAsync();
			if (chatRooms == null || chatRooms.Count == 0) return NotFound();

			return Ok(chatRooms);
		}

		/// <summary>
		/// Retrieves the details of a chat room by its unique identifier.
		/// </summary>
		/// <remarks>Only administrators or participants of the specified chat room are authorized to access its
		/// details. The response will be NotFound if the chat room does not exist, Unauthorized if the user lacks permission,
		/// or BadRequest if the identifier is invalid.</remarks>
		/// <param name="chatRoomId">The unique identifier of the chat room to retrieve. Cannot be null or empty.</param>
		/// <returns>An <see cref="ActionResult{ChatRoomResponseDto}"/> containing the chat room details if found and accessible;
		/// otherwise, an appropriate error response such as NotFound, Unauthorized, or BadRequest.</returns>
		[HttpGet("{chatRoomId}"), Authorize]
		public async Task<ActionResult<ChatRoomResponseDto>> GetChatRoomByIdAsync(string chatRoomId)
		{
			// Validate input.
			if (string.IsNullOrWhiteSpace(chatRoomId)) return BadRequest("Chat room id can not be null or empty.");

			// Check permissions.
			var user = await GetSignedInUserAsync();
			if (user == null) return StatusCode(500);
			if (user.Roles.Contains(RoleEnum.Admin) == false)
			{
				bool isParticipant = await DbContext.Participants
					.AnyAsync(p => p.ChatRoomId == chatRoomId && p.UserId == user.Id);
				if (!isParticipant) return Unauthorized("You do not have permission to access this chat room.");
			}

			// Retrieve chat room.
			var chatRoom = await DbContext.ChatRooms
				.Where(cr => cr.Id == chatRoomId)
				.Select(cr => new ChatRoomResponseDto
				{
					Id = cr.Id,
					Name = cr.Name,
					Participants = cr.Participants
						.Select(p => p.UserId)
						.ToList()
				}).FirstOrDefaultAsync();
			if (chatRoom == null) return NotFound();

			return Ok(chatRoom);
		}

		/// <summary>
		/// Updates the details of an existing chat room.
		/// </summary>
		/// <remarks>Only administrators or participants of the chat room are authorized to perform this operation.
		/// The user must be authenticated.</remarks>
		/// <param name="dto">An object containing the updated information for the chat room. The chat room ID and name must not be null or
		/// empty.</param>
		/// <returns>An HTTP response indicating the result of the update operation. Returns 200 (OK) if the update is successful, 400
		/// (Bad Request) if the input is invalid, 401 (Unauthorized) if the user does not have permission, or 404 (Not Found)
		/// if the chat room does not exist.</returns>
		[HttpPut, Authorize]
		public async Task<ActionResult> UpdateChatRoomAsync(ChatRoomUpdateDto dto)
		{
			// Validate input.
			if (dto == null) return BadRequest("Request body can not be null.");
			if (string.IsNullOrWhiteSpace(dto.Id)) return BadRequest("Chat room id can not be null or empty.");
			if (string.IsNullOrWhiteSpace(dto.Name)) return BadRequest("Chat room name can not be null or empty.");

			// Check permissions.
			var user = await GetSignedInUserAsync();
			if (user == null) return StatusCode(500);
			if (user.Roles.Contains(RoleEnum.Admin) == false)
			{
				bool isParticipant = await DbContext.Participants
					.AnyAsync(p => p.ChatRoomId == dto.Id && p.UserId == user.Id);
				if (!isParticipant) return Unauthorized("You do not have permission to access this chat room.");
			}

			// Update chat room.
			var chatRoom = await DbContext.ChatRooms
				.FirstOrDefaultAsync(cr => cr.Id == dto.Id);
			if (chatRoom == null) return NotFound();

			chatRoom.Name = dto.Name;
			await DbContext.SaveChangesAsync();

			return Ok("Chat room updated successfully.");
		}

		#endregion

		#region Participant Management

		/// <summary>
		/// Removes the signed-in user from the specified chat room.
		/// </summary>
		/// <remarks>If the user is the last participant to leave, the chat room is deleted. The user must be
		/// authenticated to perform this operation.</remarks>
		/// <param name="chatRoomId">The unique identifier of the chat room to leave. Cannot be null or empty.</param>
		/// <returns>An ActionResult indicating the outcome of the operation. Returns 200 OK if the user successfully leaves the chat
		/// room; 404 Not Found if the chat room does not exist or the user is not a participant; 400 Bad Request if the
		/// chatRoomId is invalid.</returns>
		[HttpDelete("leave/{chatRoomId}"), Authorize]
		public async Task<ActionResult> LeaveChatRoomAsync(string chatRoomId)
		{
			// Validate input
			if (string.IsNullOrWhiteSpace(chatRoomId)) return BadRequest("Chat room id can not be null or empty.");

			// Retrieve chat room with participants
			var chatRoom = await DbContext.ChatRooms
				.Include(cr => cr.Participants)
				.FirstOrDefaultAsync(cr => cr.Id == chatRoomId);
			if (chatRoom == null) return NotFound();

			var user = await GetSignedInUserAsync();
			if (user == null) return StatusCode(500);

			// Remove the participant
			var participant = chatRoom.Participants
				.FirstOrDefault(p => p.UserId == user.Id);
			if (participant == null) return NotFound("You are not a participant of this chat room.");
			chatRoom.Participants.Remove(participant);

			// Remove chat room if no participants remain.
			if (!chatRoom.Participants.Any())
			{
				DbContext.ChatRooms.Remove(chatRoom);
			}

			await DbContext.SaveChangesAsync();

			return Ok("User has left the chat room.");
		}

		/// <summary>
		/// Removes one or more participants from a specified chat room.
		/// </summary>
		/// <remarks>If all participants are removed from the chat room, the chat room itself is deleted. Only users
		/// with the 'Admin' role are authorized to perform this operation.</remarks>
		/// <param name="dto">An object containing the chat room identifier and a list of participant user IDs to remove. The chat room ID
		/// cannot be null or empty. The participant list must not be null and all user IDs must exist in the database.</param>
		/// <returns>An HTTP 200 OK result if the participants are successfully removed; otherwise, a suitable error response such as
		/// 400 Bad Request if the input is invalid or 404 Not Found if the chat room does not exist.</returns>
		[HttpPost("admin/removeParticipants"), Authorize(Roles = "Admin")]
		public async Task<ActionResult> RemoveParticipantsAsync(ParticipantRemoveDtos dto)
		{
			// Validate input.
			if (dto == null) return BadRequest("Dto body can not be null.");
			if (string.IsNullOrWhiteSpace(dto.ChatRoomId)) return BadRequest("Chat room id can not be null.");
			if (dto.ParticipantIds == null) return BadRequest("Participant list can not be empty.");
			
			// Checks if all the paticipant ids exists.
			if (await DbContext.Users
				.Where(u => dto.ParticipantIds.Contains(u.Id))
				.CountAsync() != dto.ParticipantIds.Count()
			) return BadRequest("All id's on the list must exist on the database.");

			// Retrieves the chat room with its participants.
			var chatRoom = await DbContext.ChatRooms
				.Include(cr => cr.Participants)
				.FirstOrDefaultAsync(cr => cr.Id == dto.ChatRoomId);
			if (chatRoom == null) return NotFound();

			// Removes the specified participants from the chat room.
			var participants = chatRoom.Participants
				.Where(p => dto.ParticipantIds.Contains(p.UserId))
				.ToList();
			if (participants == null) return BadRequest("All users on the list are not part of the chat room.");

			// Removes each participant from the chat room.
			foreach (var participant in participants)
			{
				chatRoom.Participants.Remove(participant);
			}

			// Remove chat room if no participants remain.
			if (!chatRoom.Participants.Any())
			{
				DbContext.ChatRooms.Remove(chatRoom);
			}

			await DbContext.SaveChangesAsync();

			return Ok("Users were removed from the chat room.");
		}

		#endregion

		#region Message Management

		/// <summary>
		/// Creates a new chat message in the specified chat room on behalf of the authenticated user.
		/// </summary>
		/// <remarks>The authenticated user must be a participant in the specified chat room to send a message. The
		/// method requires authorization.</remarks>
		/// <param name="dto">An object containing the details of the message to send, including the chat room identifier and message content.
		/// Cannot be null.</param>
		/// <returns>An <see cref="ActionResult"/> indicating the result of the operation. Returns <see cref="OkResult"/> if the
		/// message is created successfully; otherwise, returns an appropriate error result such as <see
		/// cref="BadRequestResult"/>, <see cref="UnauthorizedResult"/>, or <see cref="NotFoundResult"/>.</returns>
		[HttpPost("message"), Authorize]
		public async Task<ActionResult> SendMessageAsync(ChatMessageCreateDto dto)
		{
			// Validate input.
			if (dto == null) return BadRequest("Request body can not be empty.");
			if (string.IsNullOrWhiteSpace(dto.ChatRoomId)) return BadRequest("Chat room id can not be empty.");
			if (string.IsNullOrWhiteSpace(dto.MessageContent)) return BadRequest("There must be some message content.");

			var sender = await GetSignedInUserAsync();
			if (sender == null) return StatusCode(500);

			// Retrieves the chat room with its participants & messages.
			var chatRoom = await DbContext.ChatRooms
				.Include(cr => cr.Participants)
				.Include(cr => cr.ChatMessages)
				.FirstOrDefaultAsync(cr => cr.Id == dto.ChatRoomId);
			if (chatRoom == null) NotFound();
			if (!chatRoom.Participants.Any(p => p.UserId == sender.Id)) return Unauthorized();

			// ;ap new message & save to database.
			var message = new ChatMessage
			{
				ChatRoomId = dto.ChatRoomId,
				SenderId = sender.Id,
				MessageContent = dto.MessageContent
			};
			await DbContext.ChatMessages.AddAsync(message);
			await DbContext.SaveChangesAsync();

			return Ok("Message was created.");
		}

		/// <summary>
		/// Retrieves a paginated list of messages from a specified chat room based on the provided request parameters.
		/// </summary>
		/// <remarks>Only users who are participants in the specified chat room or have the Admin role can access the
		/// messages. The response includes a limited number of messages per request and supports cursor-based pagination for
		/// efficient retrieval of large message histories.</remarks>
		/// <param name="dto">An object containing the chat room identifier and optional pagination cursor. The chat room ID must not be null or
		/// empty.</param>
		/// <returns>An ActionResult containing a ChatMessageResponseDto with the list of chat messages, pagination information, and a
		/// cursor for retrieving additional messages if available. Returns an error response if the request is invalid or the
		/// user is not authorized.</returns>
		[HttpPost("messages/getMessages"), Authorize]
		public async Task<ActionResult<ChatMessageResponseDto>> GetAllChatRoomMessagesAsync(ChatMessageRequestDto dto)
		{
			// Validate input.
			if (string.IsNullOrWhiteSpace(dto.ChatRoomId)) return BadRequest("Chat room id can not be empty.");

			var user = await GetSignedInUserAsync();
			if (user == null) return StatusCode(500);

			// Restricting access.
			if (!user.Roles.Contains(RoleEnum.Admin))
			{
				if (!await DbContext.Participants
					.AnyAsync(p => p.ChatRoomId == dto.ChatRoomId && 
						p.UserId == user.Id)
				) return Unauthorized();
			}

			var query = DbContext.ChatMessages.Where(cm => cm.ChatRoomId == dto.ChatRoomId);

			// Apply cursor-based pagination.
			if (dto.Cursor.HasValue) query = query.Where(cm => cm.CreatedAt < dto.Cursor.Value);

			var pageSize = 2;

			// Fetch & map messages.
			var messages = await query
				.OrderByDescending(cm => cm.CreatedAt)
				.Take(pageSize + 1)
				.Select(cm => new MessageObjectDto
				{
					Id = cm.Id,
					CreatedAt = cm.CreatedAt,
					MessageContent = cm.MessageContent,
					Sender = new UserMessageDto
					{
						Id = cm.Sender.Id,
						FirstName = cm.Sender.FirstName,
						LastName = cm.Sender.LastName
					}
				})
				.ToListAsync();

			bool hasMore = messages.Count > pageSize;

			// Remove the extra message used to detect pagination.
			if (hasMore) messages.RemoveAt(messages.Count - 1);

			return Ok(new ChatMessageResponseDto
			{
				MessageObjects = messages,
				NextCursor = messages.LastOrDefault()?.CreatedAt,
				HasMore = hasMore
			});
		}

		/// <summary>
		/// Deletes a chat message with the specified identifier from the chat room.
		/// </summary>
		/// <remarks>Only administrators or the original sender who is a participant in the chat room can delete a
		/// message. The user must be authenticated to perform this operation.</remarks>
		/// <param name="messageId">The unique identifier of the chat message to remove. Must be a non-zero value.</param>
		/// <returns>An <see cref="ActionResult"/> indicating the result of the operation. Returns <see cref="OkResult"/> if the
		/// message was successfully deleted; <see cref="BadRequestResult"/> if the message ID is invalid; <see
		/// cref="UnauthorizedResult"/> if the user is not authorized; or <see cref="NotFoundResult"/> if the message does not
		/// exist.</returns>
		[HttpDelete("messages/{messageId}"), Authorize]
		public async Task<ActionResult> RemoveChatRoomeMessage(int messageId)
		{
			// Validate input.
			if (messageId == 0) return BadRequest("Message id can not be empty.");

			var user = await GetSignedInUserAsync();
			if (user == null) return Unauthorized();

			// Fetch chat messgase.
			var chatMessage = await DbContext.ChatMessages.FirstOrDefaultAsync(cm => cm.Id == messageId);
			if (chatMessage == null) return NotFound();

			// Restricting access.
			if (!user.Roles.Contains(RoleEnum.Admin))
			{
				if (!await DbContext.Participants
					.AnyAsync(p => p.ChatRoomId == chatMessage.ChatRoomId &&
						p.UserId == user.Id)
				) return Unauthorized();

				if (chatMessage.SenderId != user.Id) return Unauthorized();
			}

			DbContext.ChatMessages.Remove(chatMessage);
			await DbContext.SaveChangesAsync();

			return Ok();
		}

		#endregion
	}
}