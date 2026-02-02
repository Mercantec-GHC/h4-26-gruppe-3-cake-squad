using System;
using System.Collections.Generic;
using System.Text;

namespace Commons.Models
{
	public class ChatRoom : Common<string>
	{
		public string Name { get; set; }

		// Relations
		public List<Participant> Participants { get; set; }
		public List<ChatMessage> ChatMessages { get; set; }
	}

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