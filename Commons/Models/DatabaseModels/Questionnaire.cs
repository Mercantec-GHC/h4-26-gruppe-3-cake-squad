using Commons.Models.QuizModels;

namespace Commons.Models.DatabaseModels
{
	public class Questionnaire : Common<int>
	{
		public string UserId { get; set; }
		public Quiz? Quiz { get; set; }

        // Relations 
        public User User { get; set; }
		public List<QuestionPicture> Pictures { get; set; }
	}
}