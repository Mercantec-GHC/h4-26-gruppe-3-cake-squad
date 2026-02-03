using Commons.Enums;
using Microsoft.AspNetCore.Http;

namespace Commons.Models.Dtos
{
    public class ImageUploadDto
    {
        public string? Name { get; set; }
        public string type { get; set; }
        public IFormFile File { get; set; } = default!;

        /// <summary>
        /// Attempts to parse the current type string as a value of the PictureTypeEnum enumeration.
        /// </summary>
        /// <remarks>Parsing is case-insensitive. Returns null if the type string does not correspond to a
        /// valid PictureTypeEnum value.</remarks>
        /// <returns>A PictureTypeEnum value if the type string can be parsed successfully; otherwise, null.</returns>
        public PictureTypeEnum? GetPictureType()
        {
            if (Enum.TryParse<PictureTypeEnum>(type, true, out PictureTypeEnum result))
            {
                return result;
            }
            return null;
        }
    }

    public class ImageRenameDto
    {
        public int Id { get; set; }
        public string NewName { get; set; } = default!;
    }

    public class ImageInfoDto
    {
        public string Name { get; set; } = default!;
        public string Type { get; set; } = default!;
    }

    public class ImageInfoFullDto : ImageInfoDto
    {
        public int Id { get; set; }
        public DateTime UploadedAt { get; set; }
    }
}
