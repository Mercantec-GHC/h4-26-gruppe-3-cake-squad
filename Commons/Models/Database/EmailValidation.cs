namespace Commons.Models.Database
{
    public class EmailValidation : Common<int>
    {
        public string UserId { get; set; }
        public string ValidationCode { get; set; }
        public DateTime Expiration { get; set; }

        // Relations
        public User User { get; set; }
    }
}
