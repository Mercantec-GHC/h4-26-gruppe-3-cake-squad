using Microsoft.AspNetCore.Http;

namespace Commons.Models.Dtos
{
    public class ImageUploadDto
    {
        public string? Name { get; set; }
        public IFormFile File { get; set; } = default!;
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
