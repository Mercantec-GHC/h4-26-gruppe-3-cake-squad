namespace Commons.Models.Dtos
{
	public class UserVisibilityRequestDto
	{
		public string SourceUserId { get; set; }
		public string TargetUserId { get; set; }
		public string VisibilityEnum { get; set; }
	}
}