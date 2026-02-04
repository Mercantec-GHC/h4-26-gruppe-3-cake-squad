
namespace Commons.Models.Dtos
{
	public class ChatMessageCreateDto
	{
		public string ChatRoomId { get; set; }
		public string MessageContent { get; set; }
	}

	public class ChatMessageResponseDto
	{
		public List<MessageObjectDto> MessageObjects { get; set; }
		public DateTime? NextCursor { get; set; }
		public bool HasMore { get; set; }
	}

	public class MessageObjectDto
	{
		public int Id { get; set; }
		public DateTime CreatedAt { get; set; }
		public UserMessageDto Sender { get; set; }
		public string MessageContent { get; set; }
	}

	public class ChatMessageRequestDto
	{
		public string ChatRoomId { get; set; }
		public DateTime? Cursor { get; set; }
	}

}