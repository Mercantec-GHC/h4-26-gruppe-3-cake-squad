using Commons.Models.Database;
using Commons.Enums;

public class UserVisibility : Common<int>
{
	public string SourceUserId { get; set; }
	public string TargetUserId { get; set; }
	public UserVisibilityEnum Visibility { get; set; }

	// Relations
	public User SourceUser { get; set; }
	public User TargetUser { get; set; }
}