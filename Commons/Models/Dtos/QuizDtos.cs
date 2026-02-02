namespace Commons.Models.Dtos
{
    public class QuizSubmitDto
    {
        public string UserId { get; set; }
        public int[] Answers { get; set; }
    }

    public class QuizResultDto
    {
        public int MatchPercent { get; set; }
        public bool Passed { get; set; }
    }
}
