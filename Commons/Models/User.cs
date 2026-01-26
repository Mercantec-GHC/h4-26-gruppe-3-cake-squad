using Commons.Enums;

namespace Commons.Models
{
	public class User
	{
		public string Id { get; set; }
		public string FirstName { get; set; }
		public string LastName { get; set; }
		public DateOnly Birthday { get; set; }
		public string Description { get; set; }
		public string Email { get; set; }
		public string HashedPassword { get; set; }
		public DateOnly RegistrationDate { get; set; }
		public List<TagsEnum> ValueTags { get; set; }

		// Relations
		public List<Questionnaire> Questionnaires { get; set; }
	}
}