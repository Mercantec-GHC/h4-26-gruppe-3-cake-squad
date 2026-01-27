using Commons.Enums;

namespace Commons.Models.Quiz
{
    public class Question
    {
        public string QuestionText { get; set; }
        public QuestionTypeEnum Type { get; set; }
        public List<Option> Options { get; set; }
        public string CorrectOptionId { get; set; }
        public int Score { get; set; }
    }
}
