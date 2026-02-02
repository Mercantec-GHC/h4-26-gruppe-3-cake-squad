using Commons.Enums;

namespace Commons.Models.Database
{
	public class UserRole : Common<int>
	{
		public string UserId { get; set; }
		public RoleEnum Role { get; set; }

		// Relations
		public User User { get; set; }
    }
}