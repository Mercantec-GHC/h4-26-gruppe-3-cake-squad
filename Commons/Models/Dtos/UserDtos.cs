using Commons.Enums;

namespace Commons.Models.Dtos
{
    public class MeResponseDto
    {
        public string Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateOnly Birthday { get; set; }
        public string Email { get; set; }
        public string? Description { get; set; }
        public List<string> Tags { get; set; }
    }

    public class UserResponseDto
    {
        public string Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string? Description { get; set; }
        public List<string> Tags { get; set; }
    }

    public class UserMessageDto
    {
        public string Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }

    public class DiscoverUserResponseDto
    {
        public string Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string? Description { get; set; }
        public List<int> Pictures { get; set; }
        public List<string> Tags { get; set; }
    }
}
