namespace Commons.Models.Database
{
	public class ChatRoom : Common<string>
	{
		public string Name { get; set; }

		// Relations
		public List<Participant> Participants { get; set; }
		public List<ChatMessage> ChatMessages { get; set; }
	}
}