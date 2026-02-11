using Commons.Enums;

namespace Commons.Models.Dtos
{
	public class NotificationRequestDto
	{
		public string SenderId { get; set; }
		public string TargetId { get; set; }
		public string? ObjectId { get; set; }
		public string Content { get; set; }
		public NotificationTypeEnum Type { get; set; }
	}
}