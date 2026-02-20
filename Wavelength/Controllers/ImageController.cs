using Commons.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Wavelength.Data;
using Wavelength.Services;

namespace Wavelength.Controllers
{
    /// <summary>
    /// Provides API endpoints for uploading, retrieving, updating, and deleting user and interest-related images,
    /// including avatar management, for authenticated users.
    /// </summary>
    /// <remarks>All endpoints require authentication unless otherwise specified. The controller supports
    /// operations for both the currently signed-in user and, where applicable, for other users specified by user ID.
    /// Error responses follow standard HTTP status codes, such as 400 for invalid input, 401 for unauthorized access,
    /// 404 for not found, and 500 for server errors.</remarks>
    [ApiController]
    [Route("[controller]")]
    public class ImagesController : BaseController
    {
        private readonly ImageService imageService;

        /// <summary>
        /// Initializes a new instance of the ImagesController class with the specified database context and image
        /// service.
        /// </summary>
        /// <param name="dbContext">The database context used for accessing and managing image data.</param>
        /// <param name="imageService">The service used to perform image-related operations.</param>
        public ImagesController(AppDbContext dbContext, ImageService imageService) : base(dbContext)
        {
            this.imageService = imageService;
        }

        /// <summary>
        /// Uploads an image for the currently authenticated user.
        /// </summary>
        /// <remarks>This action requires the user to be authenticated. The uploaded image is associated
        /// with the signed-in user's account.</remarks>
        /// <param name="dto">An object containing the image data and related upload information. Cannot be null.</param>
        /// <returns>An HTTP 204 No Content response if the upload is successful; otherwise, an appropriate error response such
        /// as HTTP 400 Bad Request for invalid input or HTTP 500 Internal Server Error for server-side failures.</returns>
        [HttpPost("Upload"), Authorize]
        public async Task<ActionResult> UploadImage(ImageUploadDto dto)
        {
            try
            {
                var user = await GetSignedInUserAsync();
                if (user == null) return StatusCode(500);
                await imageService.UploadImage(user, dto);
                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while uploading the image.");
            }
        }

        /// <summary>
        /// Uploads a new avatar image for the currently authenticated user.
        /// </summary>
        /// <remarks>The user must be authenticated to upload an avatar. If the file is invalid or
        /// missing, the method returns a 400 Bad Request response with an error message. If an unexpected error occurs,
        /// a 500 Internal Server Error response is returned.</remarks>
        /// <param name="file">The image file to upload as the user's avatar. Must be a valid, non-null file in a supported image format.</param>
        /// <returns>An HTTP 204 No Content response if the upload is successful; otherwise, an appropriate error response.</returns>
        [HttpPost("Avatar/Upload"), Authorize]
        public async Task<ActionResult> UploadAvatar(IFormFile file)
        {
            try
            {
                var user = await GetSignedInUserAsync();
                if (user == null) return StatusCode(500);
                await imageService.UploadAvatar(user, file);
                return NoContent();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while uploading the avatar.");
            }
        }

        /// <summary>
        /// Returns the avatar image for the specified user or the currently signed-in user.
        /// </summary>
        /// <param name="userId">The unique identifier of the user whose avatar image to retrieve. If null, retrieves the avatar for the
        /// currently signed-in user.</param>
        /// <returns>An image file containing the user's avatar. Returns a 400 Bad Request if the user ID is invalid, a 404 Not
        /// Found if the avatar image does not exist, or a 500 Internal Server Error if an unexpected error occurs.</returns>
        [Authorize]
        [HttpGet("Avatar")]
        [HttpGet("Avatar/{userId}")]
        public async Task<ActionResult> Avatar(string? userId)
        {
            try
            {
                var user = await GetSignedInUserAsync(q => q.Include(u => u.UserVisibilities));
                if (user == null) return StatusCode(500);
                var imageData = await imageService.GetAvatarImage(user, userId);
                return File(imageData.Item1, imageData.Item2);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (FileNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while retrieving the avatar.");
            }
        }

        /// <summary>
        /// Deletes the currently signed-in user's avatar image.
        /// </summary>
        /// <remarks>This action requires the user to be authenticated. If the user does not have an
        /// avatar, a 404 Not Found response is returned. In case of unexpected errors, a 500 Internal Server Error
        /// response is provided.</remarks>
        /// <returns>A 204 No Content response if the avatar was successfully deleted; a 404 Not Found response if no avatar
        /// exists; or a 500 Internal Server Error response if an error occurs.</returns>
        [HttpDelete("Avatar"), Authorize]
        public async Task<ActionResult> DeleteAvatar()
        {
            try
            {
                var user = await GetSignedInUserAsync();
                if (user == null) return StatusCode(500);
                await imageService.DeleteAvatar(user);
                return NoContent();
            }
            catch (FileNotFoundException)
            {
                return NotFound();
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while deleting the avatar.");
            }
        }

        /// <summary>
        /// Retrieves the image associated with the specified interest.
        /// </summary>
        /// <param name="id">The unique identifier of the interest whose image is to be retrieved.</param>
        /// <param name="miniature">A value indicating whether to retrieve a miniature version of the image. If <see langword="true"/>, returns
        /// a smaller version; otherwise, returns the full-size image. If null, the default image size is returned.</param>
        /// <returns>An <see cref="ActionResult"/> containing the image file if found; otherwise, a 404 Not Found response if the
        /// image does not exist, or a 500 Internal Server Error if an unexpected error occurs.</returns>
        [HttpGet("Interest/{id}"), Authorize]
        public async Task<ActionResult> GetInterestImage(int id, bool? miniature = null)
        {
            try
            {
                var imageData = await imageService.GetInterestImage(id, miniature);
                return File(imageData.Item1, imageData.Item2);
            }
            catch (FileNotFoundException)
            {
                return NotFound();
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while retrieving the interest image.");
            }
        }

        /// <summary>
        /// Retrieves detailed information about an image associated with the specified interest identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the interest whose image information is to be retrieved.</param>
        /// <returns>An <see cref="ActionResult"/> containing the image information if found; otherwise, a 404 Not Found response
        /// if the image does not exist, or a 500 Internal Server Error response if an unexpected error occurs.</returns>
        [HttpGet("Interest/{id}/info"), Authorize]
        public async Task<ActionResult> GetImageInfo(int id)
        {
            try
            {
                var info = await imageService.GetImageInfo(id);
                return Ok(info);
            }
            catch (FileNotFoundException)
            {
                return NotFound();
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while retrieving the image info.");
            }
        }

        /// <summary>
        /// Deletes the image associated with the specified interest identifier for the currently signed-in user.
        /// </summary>
        /// <param name="id">The unique identifier of the interest image to delete.</param>
        /// <returns>A 204 No Content response if the image is successfully deleted; a 404 Not Found response if the image does
        /// not exist; a 401 Unauthorized response if the user is not authorized; or a 500 Internal Server Error
        /// response if an unexpected error occurs.</returns>
        [HttpDelete("Interest/{id}"), Authorize]
        public async Task<ActionResult> Delete(int id)
        {
            try
            {
                var user = await GetSignedInUserAsync();
                if (user == null) return StatusCode(500);
                await imageService.DeleteImage(user, id);
                return NoContent();
            }
            catch (FileNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while deleting the image.");
            }
        }

        /// <summary>
        /// Renames an existing image associated with the current user based on the provided rename information.
        /// </summary>
        /// <param name="dto">An object containing the image identifier and the new name to assign to the image. Cannot be null.</param>
        /// <returns>An HTTP 204 No Content response if the image is successfully renamed; otherwise, an appropriate error
        /// response such as 404 Not Found if the image does not exist, 401 Unauthorized if the user is not authorized,
        /// or 500 Internal Server Error for other failures.</returns>
        [HttpPut("Interest/{id}/Rename"), Authorize]
        public async Task<ActionResult> RenameImage(ImageRenameDto dto)
        {
            try
            {
                var user = await GetSignedInUserAsync();
                if (user == null) return StatusCode(500);
                await imageService.RenameImage(user, dto);
                return NoContent();
            }
            catch (FileNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while renaming the image.");
            }
        }

        /// <summary>
        /// Retrieves a list of images associated with the specified user or the currently signed-in user.
        /// </summary>
        /// <remarks>This endpoint requires authentication. If no user ID is provided, the images for the
        /// signed-in user are returned. Only authorized users can access this endpoint.</remarks>
        /// <param name="userId">The unique identifier of the user whose images to retrieve. If null, retrieves images for the currently
        /// authenticated user.</param>
        /// <returns>An <see cref="ActionResult{T}"/> containing a list of <see cref="ImageInfoFullDto"/> objects representing
        /// the user's images. Returns a 500 status code if the user cannot be determined or an error occurs.</returns>
        [Authorize]
        [HttpGet("Interest/All")]
        [HttpGet("Interest/All/{userId}")]
        public async Task<ActionResult<List<ImageInfoFullDto>>> GetUserImages(string? userId)
        {
            try
            {
                var user = await GetSignedInUserAsync();
                if (user == null) return StatusCode(500);
                var images = await imageService.GetUserImages(user, userId);
                return Ok(images);
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while retrieving the user's images.");
            }
        }
    }
}
