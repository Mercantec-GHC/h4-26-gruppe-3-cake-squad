using Commons.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wavelength.Data;
using Wavelength.Services;

namespace Wavelength.Controllers
{
    /// <summary>
    /// Provides API endpoints for user authentication, registration, profile management, and related operations.
    /// </summary>
    /// <remarks>This controller handles user authentication workflows, including registration, login, email
    /// verification, token refresh, password updates, and profile retrieval. All endpoints are routed under the
    /// "[controller]" route and use dependency-injected services for authentication logic. Some actions require the
    /// user to be authenticated via authorization attributes.</remarks>
    [ApiController]
    [Route("[controller]")]
    public class AuthController : BaseController
    {
        private readonly AuthService authService;
        private readonly OauthService oauthService;

        /// <summary>
        /// Initializes a new instance of the AuthController class with the specified database context, authentication
        /// service, and OAuth service.
        /// </summary>
        /// <param name="dbContext">The database context used for accessing application data.</param>
        /// <param name="authService">The authentication service responsible for handling user authentication operations.</param>
        /// <param name="oauthService">The OAuth service used for managing OAuth-based authentication and authorization.</param>
        public AuthController(AppDbContext dbContext, AuthService authService, OauthService oauthService) : base(dbContext)
        {
            this.authService = authService;
            this.oauthService = oauthService;
        }

        /// <summary>
        /// Registers a new user account using the specified registration details.
        /// </summary>
        /// <param name="dto">An object containing the user's registration information. Cannot be null.</param>
        /// <returns>A 201 Created result if registration is successful; otherwise, a 400 Bad Request result with an error
        /// message if the registration details are invalid.</returns>
        [HttpPost("register")]
        public async Task<ActionResult> RegisterAsync(RegisterDto dto)
        {
            try
            {
                await authService.RegisterUserAsync(dto);
                return Created();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Verifies a user's email address using the provided validation data.
        /// </summary>
        /// <param name="dto">An object containing the email verification data required to complete the verification process. Cannot be
        /// null.</param>
        /// <returns>An HTTP 200 OK result if the email is verified successfully; otherwise, an HTTP 400 Bad Request result with
        /// an error message if the verification fails.</returns>
        [HttpPost("verifyEmail")]
        public async Task<ActionResult> VerifyEmailAsync(ValidateDto dto)
        {
            try
            {
                await authService.VerifyEmailAsync(dto);
                return Ok("Email verified successfully.");
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Authenticates a user with the provided credentials and returns an authentication token if successful.
        /// </summary>
        /// <param name="dto">The login credentials and related information for the user attempting to authenticate. Cannot be null.</param>
        /// <returns>An <see cref="ActionResult{T}"/> containing an <see cref="AuthResponseDto"/> with the authentication token
        /// if the login is successful; otherwise, an unauthorized result with an error message.</returns>
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> LoginAsync(LoginDto dto)
        {
            try
            {
                var jwt = await authService.LoginAsync(dto);
                return Ok(jwt);
            }
            catch (ArgumentException ex)
            {
                return Unauthorized(ex.Message);
            }
        }

        /// <summary>
        /// Generates a new authentication token pair using the provided refresh token.
        /// </summary>
        /// <param name="dto">An object containing the refresh token and related information required to obtain new authentication tokens.
        /// Cannot be null.</param>
        /// <returns>An <see cref="ActionResult{T}"/> containing a new <see cref="AuthResponseDto"/> with refreshed
        /// authentication tokens if the refresh token is valid; otherwise, an unauthorized response.</returns>
        [HttpPost("refresh")]
        public async Task<ActionResult<AuthResponseDto>> RefreshAsync(RefreshTokenDto dto)
        {
            try
            {
                var jwt = await authService.RefreshAsync(dto);
                return Ok(jwt);
            }
            catch (ArgumentException ex)
            {
                return Unauthorized(ex.Message);
            }
        }

        /// <summary>
        /// Updates the signed-in user's email address using the provided data transfer object.
        /// </summary>
        /// <remarks>This method requires the caller to be authenticated. The email update operation may
        /// fail if the provided email address is invalid or already in use.</remarks>
        /// <param name="dto">An object containing the new email address and any required information for the update operation. Cannot be
        /// null.</param>
        /// <returns>An <see cref="ActionResult"/> indicating the outcome of the operation. Returns <see langword="Ok"/> if the
        /// email is updated successfully; <see langword="Unauthorized"/> if the user is not authenticated; or <see
        /// langword="BadRequest"/> if the input is invalid.</returns>
        [HttpPut("updateEmail"), Authorize]
        public async Task<ActionResult> UpdateEmailAsync(UpdateEmailDto dto)
        {
            try
            {
                var user = await GetSignedInUserAsync();
                if (user == null) return Unauthorized("User not authenticated.");
                await authService.UpdateUserEmailAsync(user, dto);
                return Ok("Email updated successfully.");
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Updates the signed-in user's password using the specified password information.
        /// </summary>
        /// <remarks>This action requires the user to be authenticated. The password update will fail if
        /// the provided current password is incorrect or if the new password does not meet the required
        /// criteria.</remarks>
        /// <param name="dto">An object containing the current and new password details required to update the user's password. Cannot be
        /// null.</param>
        /// <returns>An <see cref="ActionResult"/> indicating the result of the password update operation. Returns <see
        /// cref="OkObjectResult"/> if the password is updated successfully; <see cref="UnauthorizedResult"/> if the
        /// user is not authenticated; or <see cref="BadRequestObjectResult"/> if the input is invalid.</returns>
        [HttpPut("updatePassword"), Authorize]
        public async Task<ActionResult> UpdatePasswordAsync(UpdatePasswordDto dto)
        {
            try
            {
                var user = await GetSignedInUserAsync();
                if (user == null) return Unauthorized("User not authenticated.");
                await authService.UpdateUserPasswordAsync(user, dto);
                return Ok("Password updated successfully.");
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Updates the signed-in user's description with the specified information.
        /// </summary>
        /// <param name="dto">An object containing the new description details to apply to the user profile. Cannot be null.</param>
        /// <returns>An <see cref="ActionResult"/> indicating the result of the update operation. Returns 200 OK if the update is
        /// successful, 401 Unauthorized if the user is not authenticated, or 400 Bad Request if the input is invalid.</returns>
        [HttpPut("updateDescription"), Authorize]
        public async Task<ActionResult> UpdateDescriptionAsync(UpdateDescriptionDto dto)
        {
            try
            {
                var user = await GetSignedInUserAsync();
                if (user == null) return Unauthorized("User not authenticated.");
                await authService.UpdateUserDescriptionAsync(user, dto);
                return Ok("Description updated successfully.");
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Retrieves information about the currently authenticated user.
        /// </summary>
        /// <returns>An <see cref="ActionResult{MeResponseDto}"/> containing the current user's profile information if the user
        /// is authenticated; otherwise, a 500 Internal Server Error result if the user cannot be retrieved.</returns>
        [HttpGet("me"), Authorize]
        public async Task<ActionResult<MeResponseDto>> GetMeAsync()
        {
            //Get the signed-in user
            var user = await GetSignedInUserAsync();
            if (user == null) return StatusCode(500);

            //Map to MeDTO
            var meDto = MeResponseDto.FromUser(user);

            return Ok(meDto);
        }

        /// <summary>
        /// Deletes the signed-in user's account using the provided account deletion details.
        /// </summary>
        /// <remarks>This method requires the user to be authenticated. The account deletion is performed
        /// asynchronously. If the provided account deletion details are invalid, a BadRequest response is
        /// returned.</remarks>
        /// <param name="dto">An object containing information required to delete the account. Cannot be null; must include valid account
        /// deletion data as expected by the service.</param>
        /// <returns>An ActionResult indicating the outcome of the account deletion operation. Returns Ok if the account is
        /// deleted successfully; BadRequest if the request is invalid; Unauthorized if the user is not authenticated.</returns>
        [HttpDelete("deleteAccount"), Authorize]
        public async Task<ActionResult> DeleteAccountAsync(DeleteAccountDto dto)
        {
            try
            {
                var user = await GetSignedInUserAsync();
                if (user == null) return Unauthorized("User not authenticated.");
                await authService.DeleteUserAccountAsync(user, dto);
                return Ok("Account deleted successfully.");
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Handles the callback from Google's OAuth authentication flow and returns an authentication response for the
        /// user.
        /// </summary>
        /// <remarks>This endpoint is intended to be called by Google's OAuth redirect after user
        /// authorization. The returned authentication response typically includes a JWT token and user information. If
        /// the provided authorization code is invalid or missing, a bad request is returned.</remarks>
        /// <param name="request">The OAuth callback data received from Google, containing the authorization code required to complete
        /// authentication. Cannot be null.</param>
        /// <returns>An <see cref="ActionResult{AuthResponseDto}"/> containing the authentication response if the callback is
        /// valid; otherwise, a bad request result with an error message.</returns>
        [HttpPost("google/callback")]
        public async Task<ActionResult<AuthResponseDto>> GoogleCallback(OauthWebDto request)
        {
            try
            {
                var jwt = await oauthService.HandleGoogleCallback(request.Code);
                return Ok(jwt);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}