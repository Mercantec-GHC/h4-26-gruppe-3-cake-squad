namespace Commons.Models.Database
{
	public class RefreshToken : Common<string>
	{
		public string UserId { get; set; }
		public DateTime ExpiryDate { get; set; }
		public bool IsRevoked { get; set; }

		// Relations
		public User User { get; set; }
	}
}