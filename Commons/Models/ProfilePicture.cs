using Commons.Enums;

namespace Commons.Models
{
    public class ProfilePicture
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public PictureTypeEnum PictureType { get; set; }
        public string PPictureBase64 { get; set; }

        // Relations
        public User User { get; set; }
    }
}