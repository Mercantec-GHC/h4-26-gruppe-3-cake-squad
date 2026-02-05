using Commons.Enums;

namespace Commons.Models.Dtos
{
	public class UserVisibilityRequestDto
	{
		public string TargetUserId { get; set; }
		public UserVisibilityEnum VisibilityEnum { get; set; }
	}
}