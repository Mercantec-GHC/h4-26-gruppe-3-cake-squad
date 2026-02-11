using Commons.Enums;

namespace Commons.Models.Database
{
	public class Notification : Common<string>
	{
		public string SenderId { get; set; }
		public string TargetId { get; set; }
		public string? ObjectId { get; set; }
		public string Content { get; set; }
		public NotificationTypeEnum Type { get; set; }

		// Relations
		public User Sender { get; set; }
		public User Target { get; set; }
	}
}