namespace Commons.Models.DatabaseModels
{
    public class QuestionPicture : Common<int>
    {
        public int QuestionnaireId { get; set; }
        public string QPictureBase64 { get; set; }

        // Relations
        public Questionnaire Questionnaire { get; set; }
    }
}