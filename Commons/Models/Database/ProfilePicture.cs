using Commons.Enums;

namespace Commons.Models.Database
{
    public class ProfilePicture : Common<int>
    {
        public string UserId { get; set; }
        public PictureTypeEnum PictureType { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public byte[] Data { get; set; }

        // Relations
        public User User { get; set; }
    }
}