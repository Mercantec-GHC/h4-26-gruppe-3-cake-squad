using System;
using System.Collections.Generic;
using System.Text;

namespace Commons.Models
{
	public class ChatMessage : Common<int>
	{
		public string ChatRoomId { get; set; }
		public string SenderId { get; set; }
		public string MessageContent { get; set; }

		// Relations
		public User Sender { get; set; }
		public ChatRoom ChatRoom { get; set; }
	}
}