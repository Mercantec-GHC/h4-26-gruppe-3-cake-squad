namespace Commons.Models
{
    public class QuestionPicture
    {
        public int Id { get; set; }
        public int QuestionnaireId { get; set; }
        public string QPictureBase64 { get; set; }
    }
}