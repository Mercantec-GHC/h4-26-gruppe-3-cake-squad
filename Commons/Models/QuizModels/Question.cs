using Commons.Enums;

namespace Commons.Models.QuizModels
{
    public class QuestionBase
    {
        public string QuestionText { get; set; }
        public QuestionTypeEnum Type { get; set; }
        public List<Option> Options { get; set; }
    }

    public class Question : QuestionBase
    {
        public int CorrectOptionIndex { get; set; }
        public int Score { get; set; }
    }
}
