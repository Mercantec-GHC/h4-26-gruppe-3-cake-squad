using Commons.Enums;

namespace Commons.Models.Database
{
    public class ProfilePicture : Common<int>
    {
        public string UserId { get; set; }
        public PictureTypeEnum PictureType { get; set; }
        public string PPictureBase64 { get; set; }
        public string PPictureAlt { get; set; }

        // Relations
        public User User { get; set; }
    }
}