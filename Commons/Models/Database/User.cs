using Commons.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace Commons.Models.Database
{
	public class User : Common<string>
	{
		public string FirstName { get; set; }
		public string LastName { get; set; }
		public DateOnly Birthday { get; set; }
		public string? Description { get; set; }
		public string Email { get; set; }
		public string HashedPassword { get; set; }
		public List<TagsEnum> ValueTags { get; set; } = new();

		// Relations
		public Questionnaire Questionnaire { get; set; }
		public List<UserRole> UserRoles { get; set; }
		public List<ProfilePicture> ProfilePictures { get; set; }

        // Computed property for roles
        [NotMapped]
        public List<RoleEnum> Roles => (UserRoles ?? new List<UserRole>())
			.Select(ur => ur.Role)
			.Union(new[] { RoleEnum.User })
			.Distinct()
			.ToList();
    }
}