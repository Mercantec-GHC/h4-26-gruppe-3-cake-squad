namespace Commons.Models
{
    public class QuizScore : Common<int>
    {
        public string PlayerId { get; set; }
        public string QuizOwnerId { get; set; }
        public int MatchPercent { get; set; }
        public bool IsUserVisible { get; set; }

        // Relations
        public User Player { get; set; }
        public User QuizOwner { get; set; }
    }
}
