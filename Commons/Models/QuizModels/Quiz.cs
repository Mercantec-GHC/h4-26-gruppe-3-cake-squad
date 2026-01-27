namespace Commons.Models.QuizModels
{
    public class Quiz
    {
        public int ScoreRequired { get; set; }
        public List<Question> Questions { get; set; }
    }
}
