using Commons.Enums;
using Commons.Models.Database;
using Commons.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wavelength.Data;
using Wavelength.Services;

namespace Wavelength.Controllers
{
	/// <summary>
	/// Provides API endpoints for managing chat rooms, participants, and messages within the application. Supports
	/// operations such as creating and updating chat rooms, sending and retrieving messages, and managing chat
	/// participants.
	/// </summary>
	/// <remarks>All endpoints require authentication, and certain operations are restricted to users with specific
	/// roles (such as Admin) or to participants of a chat room. Message content is encrypted before storage to ensure
	/// privacy. The controller enforces input validation and access control for all chat-related actions.</remarks>
	[ApiController]
	[Route("[controller]")]
	public class ChatController : BaseController
	{
		private readonly AesEncryptionService aesService;
		private readonly NotificationService notificationService;
		private readonly ChatService chatService;

		/// <summary>
		/// Initializes a new instance of the ChatController class with the specified database context, encryption service,
		/// and notification service.
		/// </summary>
		/// <remarks>Use this constructor to provide required dependencies for chat operations, including data access,
		/// message encryption, and user notifications. All parameters must be valid and non-null to ensure proper controller
		/// functionality.</remarks>
		/// <param name="dbContext">The database context used for accessing and managing chat-related data.</param>
		/// <param name="aesService">The AES encryption service used to secure chat messages and sensitive information.</param>
		/// <param name="notificationService">The notification service responsible for sending chat notifications to users.</param>
		public ChatController(AppDbContext dbContext, AesEncryptionService aesService, NotificationService notificationService, ChatService chatService) : base(dbContext)
		{
			this.aesService = aesService;
			this.notificationService = notificationService;
			this.chatService = chatService;
		}

		#region Chat Room Management

		/// <summary>
		/// Creates a new chat room with the specified details and participants.
		/// </summary>
		/// <remarks>This method requires the user to be authenticated. If the request body is null or invalid, a
		/// BadRequest response is returned. Exceptions encountered during the creation process are also handled and result in
		/// a BadRequest response.</remarks>
		/// <param name="dto">A data transfer object containing the details required to create the chat room, including the room name and a list
		/// of participant IDs. The room name must not be null or empty, and at least one participant must be specified.</param>
		/// <returns>An ActionResult that indicates the outcome of the chat room creation. Returns Ok() if the chat room is created
		/// successfully; otherwise, returns a BadRequest with an error message.</returns>
		[HttpPost, Authorize]
		public async Task<ActionResult> CreateChatRoomAsync(ChatRoomCreateDto dto)
		{
			try
			{
				if (dto == null) return BadRequest("Request body can not be null.");
				if (string.IsNullOrWhiteSpace(dto.RoomName)) return BadRequest("RoomName can not be null or empty.");
				if (dto.ParticipantIds == null || dto.ParticipantIds.Count == 0) return BadRequest("At least one participant must be chosen.");

				var creator = await GetSignedInUserAsync(q => q.Include(u => u.UserVisibilities));
				if (creator == null) return StatusCode(500);

				await chatService.CreateChatRoomAsync(dto, creator);
				return Ok();
			}
			catch (Exception ex)
			{
				return BadRequest($"Failed to create chat room: {ex.Message}");
			}
		}

		/// <summary>
		/// Retrieves all available chat rooms asynchronously.
		/// </summary>
		/// <remarks>This method requires the caller to have 'Admin' role permissions. If an error occurs during the
		/// retrieval process, a bad request response is returned with an error message.</remarks>
		/// <returns>A list of <see cref="ChatRoomResponseDto"/> objects representing the chat rooms. Returns an empty list if no chat
		/// rooms are available.</returns>
		[HttpGet, Authorize(Roles = "Admin")]
		public async Task<ActionResult<List<ChatRoomResponseDto>>> GetAllAsync()
		{
			try
			{
				return Ok(await chatService.GetAllAsync());
			}
			catch (Exception ex)
			{
				return BadRequest($"Failed to retrieve chat rooms: {ex.Message}");
			}
		}

		/// <summary>
		/// Retrieves the details of a chat room identified by the specified chat room ID.
		/// </summary>
		/// <remarks>This method requires the caller to be authenticated. If the user is not signed in, a 500 status
		/// code is returned. If the chat room ID is invalid, a BadRequest response is returned.</remarks>
		/// <param name="chatRoomId">The unique identifier of the chat room to retrieve. This value must not be null, empty, or consist only of
		/// white-space characters.</param>
		/// <returns>An ActionResult containing a ChatRoomResponseDto with the chat room details if found; otherwise, a BadRequest
		/// result if the chat room ID is invalid, or a 500 status code if the user is not authenticated.</returns>
		[HttpGet("{chatRoomId}"), Authorize]
		public async Task<ActionResult<ChatRoomResponseDto>> GetChatRoomByIdAsync(string chatRoomId)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(chatRoomId)) return BadRequest("Chat room id can not be null or empty.");

				var user = await GetSignedInUserAsync();
				if (user == null) return StatusCode(500);

				return Ok(await chatService.GetChatRoomByIdAsync(chatRoomId, user));
			}
			catch (Exception ex)
			{
				return BadRequest($"Failed to retrieve chat room: {ex.Message}");
			}
		}

		/// <summary>
		/// Updates the details of an existing chat room with the specified information.
		/// </summary>
		/// <remarks>This method requires the caller to be authorized. The request will fail if the chat room
		/// identifier or name is missing or empty. An error response is returned if the update cannot be completed.</remarks>
		/// <param name="dto">An object containing the updated chat room information, including the unique identifier and the new name. The
		/// identifier and name must not be null or empty.</param>
		/// <returns>An ActionResult that indicates the result of the update operation. Returns Ok if the update is successful;
		/// otherwise, returns BadRequest with an error message.</returns>
		[HttpPut, Authorize]
		public async Task<ActionResult> UpdateChatRoomAsync(ChatRoomUpdateDto dto)
		{
			try
			{
				if (dto == null) return BadRequest("Request body can not be null.");
				if (string.IsNullOrWhiteSpace(dto.Id)) return BadRequest("Chat room id can not be null or empty.");
				if (string.IsNullOrWhiteSpace(dto.Name)) return BadRequest("Chat room name can not be null or empty.");

				// Check permissions.
				var user = await GetSignedInUserAsync();
				if (user == null) return StatusCode(500);

				await chatService.UpdateChatRoomAsync(dto, user);
				return Ok();
			}
			catch (Exception ex)
			{
				return BadRequest($"Failed to update chat room: {ex.Message}");
			}
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

			// Filters the participants to be removed to only include those who are currently part of the chat room.
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
		/// Sends a new message to the specified chat room asynchronously.
		/// </summary>
		/// <remarks>This method requires the user to be authenticated. The request will fail if the input data is
		/// invalid or if the user cannot be identified.</remarks>
		/// <param name="dto">A data transfer object containing the details of the message to send, including the chat room identifier and
		/// message content. The chat room identifier must not be null or empty, and the message content must contain text.</param>
		/// <returns>An ActionResult that indicates the result of the operation. Returns Ok if the message is sent successfully;
		/// otherwise, returns a BadRequest with an error message.</returns>
		[HttpPost("message"), Authorize]
		public async Task<ActionResult> SendMessageAsync(ChatMessageCreateDto dto)
		{
			try
			{
				if (dto == null) return BadRequest("Request body can not be empty.");
				if (string.IsNullOrWhiteSpace(dto.ChatRoomId)) return BadRequest("Chat room id can not be empty.");
				if (string.IsNullOrWhiteSpace(dto.MessageContent)) return BadRequest("There must be some message content.");

				var sender = await GetSignedInUserAsync();
				if (sender == null) return StatusCode(500);

				await chatService.SendMessageAsync(dto, sender);
				return Ok();
			}
			catch (Exception ex)
			{
				return BadRequest($"Failed to create message: {ex.Message}");
			}
		}

		/// <summary>
		/// Retrieves a paginated list of messages from a specified chat room.
		/// </summary>
		/// <remarks>Only users who are participants in the chat room or have the Admin role can access the messages.
		/// The response includes a cursor for pagination and indicates if more messages are available.</remarks>
		/// <param name="dto">An object containing the chat room identifier and optional pagination cursor. The chat room ID must not be null or
		/// empty.</param>
		/// <returns>An <see cref="ActionResult{ChatMessageResponseDto}"/> containing the list of chat messages for the specified chat
		/// room, along with pagination information. Returns an error response if the request is invalid or the user is not
		/// authorized.</returns>
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
					MessageContent = aesService.Decrypt(cm.MessageContent),
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
		public async Task<ActionResult> RemoveChatRoomeMessageAsync(int messageId)
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