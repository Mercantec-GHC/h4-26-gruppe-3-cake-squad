using Commons.Enums;

namespace Commons.Models.Dtos
{
	public class NotificationRequestDto
	{
		public string SenderId { get; set; }
		public List<string> TargetIds { get; set; }
		public string Content { get; set; }
	}

	public class AdminNotificationRequestDto
	{
		public List<string> TargetIds { get; set; }
		public string Content { get; set; }
	}

	public class MessageNotificationRequestDto
	{
		public string SenderId { get; set; }
		public string ChatRoomId { get; set; }
		public string ObjectId { get; set; }
		public string Content { get; set; }
	}

	public class NotificationResponseDto
	{
		public string Id { get; set; }
		public string SenderId { get; set; }
		public string? ObjectId { get; set; }
		public string Content { get; set; }
		public NotificationTypeEnum Type { get; set; }
		public DateTime CreatedAt { get; set; }
	}
}