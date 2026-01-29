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
}