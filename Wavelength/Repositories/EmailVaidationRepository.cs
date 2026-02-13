using Commons.Models.Database;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using Wavelength.Data;

namespace Wavelength.Repositories
{
    /// <summary>
    /// Provides methods for creating and validating email verification codes used in user registration and password
    /// reset workflows.
    /// </summary>
    /// <remarks>This class manages the lifecycle of email validation codes, ensuring that each code is unique
    /// and expires after a set period. It is intended to be used with an application database context to persist and
    /// validate email verification entries.</remarks>
    public class EmailVaidationRepository
    {
        private const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        private readonly AppDbContext dbContext;

        /// <summary>
        /// Initializes a new instance of the EmailVaidationAccess class using the specified database context.
        /// </summary>
        /// <param name="dbContext">The database context to be used for accessing email validation data. Cannot be null.</param>
        public EmailVaidationRepository(AppDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        /// <summary>
        /// Creates a new email validation entry for the specified user and saves it to the database.
        /// </summary>
        /// <remarks>The generated email validation code is guaranteed to be unique within the database.
        /// The validation entry will expire 24 hours after creation.</remarks>
        /// <param name="userId">The unique identifier of the user for whom the email validation is being created. Cannot be null or empty.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the created <see
        /// cref="EmailValidation"/> entry.</returns>
        public async Task<EmailValidation> CreateEmailValidationAsync(string userId)
        {
            // Create a new email validation entry
            var validation = new EmailValidation
            {
                UserId = userId,
                Expiration = DateTime.UtcNow.AddHours(24)
            };

            // Ensure the generated code is unique
            var code = string.Empty;
            do
            {
                code = GenerateCode();
            }
            while (await dbContext.EmailValidations.AnyAsync(ev => ev.ValidationCode == code));
            validation.ValidationCode = code;

            // Save the validation entry to the database
            await dbContext.EmailValidations.AddAsync(validation);
            await dbContext.SaveChangesAsync();

            return validation;
        }

        /// <summary>
        /// Validates the specified email verification code and marks the associated user's email as verified if the
        /// code is valid and not expired.
        /// </summary>
        /// <remarks>If the code is valid and not expired, the user's email is marked as verified and the
        /// code is removed from the database. If the code is invalid or expired, no changes are made.</remarks>
        /// <param name="code">The email verification code to validate. Cannot be null.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains <see langword="true"/> if the
        /// code is valid and the user's email is verified; otherwise, <see langword="false"/>.</returns>
        public async Task<bool> ValidateCodeAsync(string code)
        {
            // Check if the code exists and is not expired
            var validation = await dbContext.EmailValidations
                .Include(ev => ev.User)
                .FirstOrDefaultAsync(ev => ev.ValidationCode == code);

            // If the code is invalid or expired, return false
            if (validation == null || validation.Expiration < DateTime.UtcNow)
                return false;

            // Mark the user's email as verified
            validation.User.IsEmailVerified = true;

            // If the code is valid, remove it from the database
            var validations = await dbContext.EmailValidations.Where(ev => ev.UserId == validation.UserId).ToListAsync();
            dbContext.EmailValidations.RemoveRange(validations);

            await dbContext.SaveChangesAsync();

            return true;
        }

        private static string GenerateCode(int length = 6)
        {
            var data = RandomNumberGenerator.GetBytes(length);
            var result = new char[length];

            for (int i = 0; i < length; i++)
            {
                result[i] = chars[data[i] % chars.Length];
            }

            return new string(result);
        }

    }
}
