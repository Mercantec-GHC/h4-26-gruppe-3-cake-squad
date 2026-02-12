namespace Commons.Models.Dtos
{
    public class RegisterDto
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public DateOnly Birthday { get; set; }
    }

    public class ValidateDto
    {
        public string Code { get; set; }
    }

    public class LoginDto
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public class AuthResponseDto
    {
        public string JwtToken { get; set; }
        public string RefreshToken { get; set; }
        public int Expires { get; set; }
    }

    public class RefreshTokenDto
    {
        public string Token { get; set; }
    }

    public class UpdatePasswordDto
    {
        public string CurrentPassword { get; set; }
        public string NewPassword { get; set; }
        public string ConfirmNewPassword { get; set; }
    }

    public class UpdateDescriptionDto
    {
        public string? Description { get; set; }
    }

	public class OauthDto
	{
		public string Provider { get; set; }
        public string Code { get; set; }
	}
}
