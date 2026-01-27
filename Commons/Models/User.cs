using Commons.Enums;

namespace Commons.Models
{
	public class User : Common<string>
	{
		public string FirstName { get; set; }
		public string LastName { get; set; }
		public DateOnly Birthday { get; set; }
		public string Description { get; set; }
		public string Email { get; set; }
		public string HashedPassword { get; set; }
		public List<TagsEnum> ValueTags { get; set; }

		// Relations
		public Questionnaire Questionnaire { get; set; }
		public List<UserRole> UserRoles { get; set; }
		public List<ProfilePicture> ProfilePictures { get; set; }
    }
}