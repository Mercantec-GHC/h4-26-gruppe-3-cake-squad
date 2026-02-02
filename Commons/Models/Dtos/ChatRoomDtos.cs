namespace Commons.Models.Dtos
{
	public class CreateChatRoomDto
	{
		public List<string> ParticipantIds { get; set; }
		public string RoomName { get; set; }
	}

	public class GetChatRoomDto
	{
		public string Id { get; set; }
		public string Name { get; set; }
		public List<string> Participants { get; set; }
	}
}