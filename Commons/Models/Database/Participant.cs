namespace Commons.Models.Database
{
	public class Participant : Common<int>
	{
		public string UserId { get; set; }
		public string ChatRoomId { get; set; }

		// Relations
		public User User { get; set; }
		public ChatRoom ChatRoom { get; set; }
	}
}