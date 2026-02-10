using Commons.Enums;
using Commons.Models.Database;

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

        /// <summary>
        /// Creates a new instance of <see cref="MeResponseDto"/> populated with data from the specified <see
        /// cref="User"/> object.
        /// </summary>
        /// <param name="user">The user whose information is used to populate the response. Cannot be null.</param>
        /// <returns>A <see cref="MeResponseDto"/> containing the user's ID, name, birthday, email, description, and associated
        /// tags.</returns>
        public static MeResponseDto FromUser(User user)
        {
            return new MeResponseDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Birthday = user.Birthday,
                Email = user.Email,
                Description = user.Description,
                Tags = user.ValueTags.Select(t => t.ToString()).ToList()
            };
        }
    }

    public class UserResponseDto
    {
        public string Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string? Description { get; set; }
        public List<string> Tags { get; set; }

        /// <summary>
        /// Creates a new UserResponseDto instance from the specified User entity.
        /// </summary>
        /// <param name="user">The User entity to convert. Cannot be null.</param>
        /// <returns>A UserResponseDto populated with data from the specified User.</returns>
        public static UserResponseDto FromUser(User user)
        {
            return new UserResponseDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Description = user.Description,
                Tags = user.ValueTags.Select(t => t.ToString()).ToList()
            };
        }
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
