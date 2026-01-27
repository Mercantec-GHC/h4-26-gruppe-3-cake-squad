using Commons.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Commons.Models
{
	public class UserRole : Common<int>
	{
		public string UserId { get; set; }
		public RoleEnum Role { get; set; }
	}
}