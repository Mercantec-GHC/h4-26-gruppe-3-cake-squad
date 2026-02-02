namespace Commons.Models.Dtos
{
	public class ChatRoomCreateDto
	{
		public List<string> ParticipantIds { get; set; }
		public string RoomName { get; set; }
	}

	public class ChatRoomResponseDto
	{
		public string Id { get; set; }
		public string Name { get; set; }
		public List<string> Participants { get; set; }
	}

	public class ChatRoomUpdateDto
	{
		public string Id { get; set; }
		public string Name { get; set; }
	}
}