using Commons.Models.Database;
using Commons.Models.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using Wavelength.Data;
using Wavelength.Repositories;

namespace Wavelength.Services
{
    /// <summary>
    /// Provides authentication and user account management services, including user registration, login, email
    /// verification, password updates, and user profile updates.
    /// </summary>
    /// <remarks>The AuthService class encapsulates core authentication workflows such as user registration,
    /// login with JWT token issuance, email verification, password management, and profile updates. It coordinates with
    /// the database context, JWT service, and email validation repository to perform these operations. All methods are
    /// asynchronous and designed to be used in web application scenarios. This class is not thread-safe; each instance
    /// should be used per request or with appropriate synchronization.</remarks>
    public class AuthService
    {
        private readonly AppDbContext dbContext;
        private readonly JwtService jwtService;
        private readonly EmailVaidationRepository emailVaidation;
        private readonly MailService mailService;

        /// <summary>
        /// Initializes a new instance of the AuthService class with the specified dependencies required for
        /// authentication and email validation operations.
        /// </summary>
        /// <param name="dbContext">The database context used for accessing and managing user data.</param>
        /// <param name="jwtService">The service used to generate and validate JSON Web Tokens (JWT) for authentication.</param>
        /// <param name="emailVaidation">The repository used to manage email validation processes.</param>
        /// <param name="mailService">The service used to send email messages for authentication and validation purposes.</param>
        public AuthService(AppDbContext dbContext, JwtService jwtService, EmailVaidationRepository emailVaidation, MailService mailService)
        {
            this.dbContext = dbContext;
            this.jwtService = jwtService;
            this.emailVaidation = emailVaidation;
            this.mailService = mailService;
        }

        /// <summary>
        /// Registers a new user with the provided registration details asynchronously. Validates user input and creates
        /// a new user account if all requirements are met.
        /// </summary>
        /// <remarks>If the provided email address is already associated with an unverified user, the
        /// existing unverified user will be removed and registration will proceed. After successful registration, an
        /// email validation entry is created for the new user.</remarks>
        /// <param name="dto">An object containing the user's registration information, including first name, last name, email address,
        /// password, and birthday.</param>
        /// <returns>A task that represents the asynchronous registration operation.</returns>
        /// <exception cref="ArgumentException">Thrown if any of the registration details are invalid, such as missing required fields, invalid email
        /// format, password not meeting security requirements, user is under 18 years old, or the email is already in
        /// use by a verified account.</exception>
        public async Task RegisterUserAsync(RegisterDto dto)
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(dto.FirstName)) throw new ArgumentException("First name is required.");
            if (string.IsNullOrWhiteSpace(dto.LastName)) throw new ArgumentException("Last name is required.");
            if (!Regex.Matches(dto.Email, "^[\\w-\\.]+@([\\w-]+\\.)+[\\w-]{2,4}$").Any()) throw new ArgumentException("Invalid email format.");
            if (dto.Password.Length < 8) throw new ArgumentException("Password must be at least 8 characters long.");
            if (!IsPasswordSecure(dto.Password)) throw new ArgumentException("Password must contain at least one uppercase letter, one lowercase letter, one digit, and one special character.");
            if (dto.Birthday >= DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-18))) throw new ArgumentException("You must be at least 18 years old to register.");

            // Check if email is already in use
            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == dto.Email.ToLower());
            if (user != null)
            {
                if (user.IsEmailVerified) throw new ArgumentException("Email already in use.");

                // If the email is not verified, we can allow re-registration by removing the existing unverified user
                dbContext.Remove(user);
                await dbContext.SaveChangesAsync();
            }

            // Create new user
            var newUser = new User
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = dto.Email.ToLower(),
                Birthday = dto.Birthday,
                HashedPassword = BCrypt.Net.BCrypt.HashPassword(dto.Password)
            };

            // Save to database
            await dbContext.Users.AddAsync(newUser);
            await dbContext.SaveChangesAsync();

            // Create email validation entry
            var validation = await emailVaidation.CreateEmailValidationAsync(newUser.Id);

            // Send validation email
            var body = mailService.RenderTemplate("RegistrationMail", new Dictionary<string, string>
            {
                { "Name", $"{newUser.FirstName} {newUser.LastName}" },
                { "Date", DateTime.Now.ToString("MMMM dd, yyyy") },
                { "Code", validation.ValidationCode }
            });
            mailService.SendEmail(newUser.Email, "Velkommen til Wavelength", body);
        }

        /// <summary>
        /// Asynchronously verifies an email address using the provided validation code.
        /// </summary>
        /// <param name="dto">An object containing the email verification code to validate. The code must be valid and not expired.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">Thrown if the validation code is invalid or has expired.</exception>
        public async Task VerifyEmailAsync(ValidateDto dto)
        {
            // Validate the email verification code
            bool validation = await emailVaidation.ValidateCodeAsync(dto.Code);

            // If validation fails, throw an exception
            if (!validation) throw new ArgumentException("Invalid or expired validation code.");
        }

        /// <summary>
        /// Authenticates a user with the specified credentials and returns an authentication response containing a JWT
        /// token and related information.
        /// </summary>
        /// <param name="dto">An object containing the user's login credentials, including email and password. The email must correspond
        /// to a registered and verified user.</param>
        /// <returns>An <see cref="AuthResponseDto"/> containing the authentication token and user details if the login is
        /// successful.</returns>
        /// <exception cref="ArgumentException">Thrown if the email is not associated with a verified user or if the password is incorrect.</exception>
        public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
        {
            //Find user with email and include UserRoles for JWT claims
            var user = await dbContext.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Email == dto.Email.ToLower() && u.IsEmailVerified);

            //If user not found or password incorrect, throw exception
            if (user == null)
                throw new ArgumentException("Invalid email or password.");

            // Verify password, throw exception if incorrect
            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.HashedPassword))
                throw new ArgumentException("Invalid email or password.");

            return await jwtService.CreateAuthResponseAsync(user);
        }

        /// <summary>
        /// Generates a new authentication response using the provided refresh token.
        /// </summary>
        /// <param name="dto">An object containing the refresh token to validate and use for generating a new authentication response. The
        /// token must be valid and not expired.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains an authentication response with
        /// new access and refresh tokens.</returns>
        /// <exception cref="ArgumentException">Thrown if the refresh token is invalid or expired.</exception>
        public async Task<AuthResponseDto> RefreshAsync(RefreshTokenDto dto)
        {
            var user = await jwtService.ValidateRefreshTokenAsync(dto.Token);
            if (user == null) throw new ArgumentException("Invalid or expired refresh token.");

            return await jwtService.CreateAuthResponseAsync(user);
        }

        /// <summary>
        /// Asynchronously updates the specified user's password after validating the current password and the new
        /// password requirements.
        /// </summary>
        /// <remarks>The new password must be at least 8 characters long and contain at least one
        /// uppercase letter, one lowercase letter, one digit, and one special character. The operation updates the
        /// user's password and the last updated timestamp in the database.</remarks>
        /// <param name="user">The user whose password is to be updated. Must not be null.</param>
        /// <param name="dto">An object containing the current password, new password, and confirmation of the new password. All fields
        /// must be provided and meet password policy requirements.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">Thrown if the current password is incorrect, if the new password does not meet security requirements, or if
        /// the new password and confirmation do not match.</exception>
        public async Task UpdateUserPasswordAsync(User user, UpdatePasswordDto dto)
        {
            // Validate current password
            if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.HashedPassword))
                throw new ArgumentException("Current password is incorrect.");

            // Validate new password
            if (dto.NewPassword.Length < 8) throw new ArgumentException("Password must be at least 8 characters long.");
            if (!IsPasswordSecure(dto.NewPassword)) throw new ArgumentException("Password must contain at least one uppercase letter, one lowercase letter, one digit, and one special character.");

            // Validate password confirmation
            if (dto.NewPassword != dto.ConfirmNewPassword)
                throw new ArgumentException("New password and confirmation password do not match.");

            // Update password
            user.HashedPassword = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;

            dbContext.Users.Update(user);
            await dbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Asynchronously updates the user's description with the specified value.
        /// </summary>
        /// <param name="user">The user whose description is to be updated. Cannot be null.</param>
        /// <param name="dto">An object containing the new description value. The description must not exceed 500 characters.</param>
        /// <returns>A task that represents the asynchronous update operation.</returns>
        /// <exception cref="ArgumentException">Thrown if the description exceeds 500 characters.</exception>
        public async Task UpdateUserDescriptionAsync(User user, UpdateDescriptionDto dto)
        {
            if (dto.Description != null)
            {
                // Trim description to remove leading and trailing whitespace
                dto.Description = dto.Description.Trim();

                // Validate description
                if (dto.Description.Length > 500)
                    throw new ArgumentException("Description cannot exceed 500 characters.");
                if (string.IsNullOrEmpty(dto.Description)) dto.Description = null;
            }

            // Update description
            user.Description = dto.Description ?? string.Empty;
            user.UpdatedAt = DateTime.UtcNow;

            dbContext.Users.Update(user);
            await dbContext.SaveChangesAsync();
        }

        /// <summary>
		/// Checks if the password is secure, using <see cref="Regex"/>.
		/// </summary>
		/// <param name="password"></param>
		/// <returns><see cref="bool"/> of true, if the given password is secure.</returns>
		private static bool IsPasswordSecure(string password)
        {
            if (string.IsNullOrEmpty(password)) return false;

            // Regex to check if the password meets the following criteria:
            // ^               - Ensures the match starts at the beginning of the string.
            // (?=.*[A-Z])     - Asserts that there is at least one uppercase letter in the string.
            // (?=.*[a-z])     - Asserts that there is at least one lowercase letter in the string.
            // (?=.*\d)        - Asserts that there is at least one digit (number) in the string.
            // (?=.*[\W_])     - Asserts that there is at least one special character (non-word character or underscore).
            // [^\s]{8,}       - Ensures the string is at least 8 characters long and does not contain any whitespace.
            // $               - Ensures the match ends at the end of the string.
            var regex = new Regex(@"^(?=.*[A-Z])(?=.*[a-z])(?=.*\d)(?=.*[\W_])[^\s]{8,}$");
            return regex.IsMatch(password);
        }
    }
}
