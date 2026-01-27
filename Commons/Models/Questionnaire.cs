using System;
using System.Collections.Generic;
using System.Text;

namespace Commons.Models
{
	public class Questionnaire : Common<int>
	{
		public string UserId { get; set; }
		public string Questions { get; set; }
	
		// Relations 
		public User User { get; set; }
	}
}