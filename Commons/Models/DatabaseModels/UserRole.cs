using Commons.Enums;

namespace Commons.Models.DatabaseModels
{
	public class UserRole : Common<int>
	{
		public string UserId { get; set; }
		public RoleEnum Role { get; set; }

		// Relations
		public User User { get; set; }
    }
}