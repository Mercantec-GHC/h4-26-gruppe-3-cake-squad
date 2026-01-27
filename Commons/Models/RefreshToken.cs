using System;
using System.Collections.Generic;
using System.Text;

namespace Commons.Models
{
	public class RefreshToken
	{
		public string Id { get; set; }
		public string UserId { get; set; }
		public DateTime ExpiryDate { get; set; }
		public bool IsRevoked { get; set; }

		// Relations
		public User User { get; set; }
	}
}
