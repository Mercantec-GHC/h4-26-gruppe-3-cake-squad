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
		public ChatController(AppDbContext context, ChatService chatService) : base(context)
		{
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
		/// Removes the currently authenticated user from the specified chat room.
		/// </summary>
		/// <remarks>This action requires the user to be authenticated. If the user is not signed in, a 500 Internal
		/// Server Error is returned.</remarks>
		/// <param name="chatRoomId">The unique identifier of the chat room to leave. This value cannot be null, empty, or consist only of white-space
		/// characters.</param>
		/// <returns>An <see cref="ActionResult"/> that indicates the result of the operation. Returns <see cref="OkResult"/> if the
		/// user successfully leaves the chat room; otherwise, returns <see cref="BadRequestResult"/> if the chat room ID is
		/// invalid or <see cref="StatusCodeResult"/> with status code 500 if the user is not authenticated.</returns>
		[HttpDelete("leave/{chatRoomId}"), Authorize]
		public async Task<ActionResult> LeaveChatRoomAsync(string chatRoomId)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(chatRoomId)) return BadRequest("Chat room id can not be null or empty.");

				var user = await GetSignedInUserAsync();
				if (user == null) return StatusCode(500);

				await chatService.LeaveChatRoomAsync(chatRoomId, user);
				return Ok();
			}
			catch (Exception ex)
			{
				return BadRequest($"Failed to create chat room: {ex.Message}");
			}
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
			try
			{
				if (dto == null) return BadRequest("Dto body can not be null.");
				if (string.IsNullOrWhiteSpace(dto.ChatRoomId)) return BadRequest("Chat room id can not be null.");
				if (dto.ParticipantIds == null) return BadRequest("Participant list can not be empty.");

				await chatService.AdminRemoveParticipantsAsync(dto);
				return Ok("Users were removed from the chat room.");
			}
			catch (Exception ex)
			{
				return BadRequest($"Failed to remove participants: {ex.Message}");
			}
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
		/// Retrieves all messages from the specified chat room.
		/// </summary>
		/// <remarks>This method requires the caller to be authenticated. Ensure that the chat room ID provided in the
		/// request is valid before calling this method.</remarks>
		/// <param name="dto">A request object that contains the identifier of the chat room for which to retrieve messages. The chat room ID
		/// must not be null, empty, or consist only of white-space characters.</param>
		/// <returns>An ActionResult containing a ChatMessageResponseDto with the list of messages from the specified chat room.
		/// Returns a BadRequest result if the chat room ID is invalid or if an error occurs during retrieval. Returns a 500
		/// status code if the user is not authenticated.</returns>
		[HttpPost("messages/getMessages"), Authorize]
		public async Task<ActionResult<ChatMessageResponseDto>> GetAllChatRoomMessagesAsync(ChatMessageRequestDto dto)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(dto.ChatRoomId)) return BadRequest("Chat room id can not be empty.");

				var user = await GetSignedInUserAsync();
				if (user == null) return StatusCode(500);
				
				return Ok(await chatService.GetAllChatRoomMessagesAsync(dto, user));	
			}
			catch (Exception ex)
			{
				return BadRequest($"Failed to retrieve messages: {ex.Message}");
			}
		}

		/// <summary>
		/// Removes a chat room message identified by the specified message ID.
		/// </summary>
		/// <remarks>This method requires the user to be authenticated. If the message ID is zero or the user is not
		/// signed in, appropriate error responses are returned.</remarks>
		/// <param name="messageId">The unique identifier of the message to be removed. Must be a positive integer.</param>
		/// <returns>An ActionResult indicating the outcome of the operation. Returns Ok if the message is successfully removed;
		/// otherwise, returns a BadRequest with an error message.</returns>
		[HttpDelete("messages/{messageId}"), Authorize]
		public async Task<ActionResult> RemoveChatRoomeMessageAsync(int messageId)
		{
			try
			{
				if (messageId == 0) return BadRequest("Message id can not be empty.");

				var user = await GetSignedInUserAsync();
				if (user == null) return Unauthorized();

				await chatService.RemoveChatRoomMessageAsync(messageId, user);
				return Ok();
			}
			catch (Exception ex)
			{
				return BadRequest($"Failed to remove message: {ex.Message}");
			}
		}

		#endregion
	}
}