using Commons.Enums;
using Commons.Models.Database;
using Commons.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Wavelength.Data;
using Wavelength.Helpers;

namespace Wavelength.Controllers
{
    /// <summary>
    /// Provides API endpoints for uploading, retrieving, and deleting user profile images.
    /// </summary>
    /// <remarks>This controller requires authentication for certain actions, such as uploading and deleting
    /// profile images. It supports common image formats including JPEG, PNG, GIF, and WebP. All operations are
    /// performed in the context of the currently authenticated user, and access to profile images is restricted based
    /// on ownership where applicable.</remarks>
    [ApiController]
    [Route("[controller]")]
    public class ProfileImageController : ControllerBase
    {
        private readonly AppDbContext dbContext;

        /// <summary>
        /// Initializes a new instance of the ProfileImageController class using the specified database context.
        /// </summary>
        /// <param name="dbContext">The database context to be used for accessing and managing profile image data. Cannot be null.</param>
        public ProfileImageController(AppDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        /// <summary>
        /// Uploads a new profile image for the currently authenticated user.
        /// </summary>
        /// <remarks>The user must be authenticated to upload an image. The method validates the file type
        /// and content to ensure only supported image formats are accepted. If the upload is successful, the image is
        /// stored and associated with the user's profile.</remarks>
        /// <param name="dto">An object containing the image file and related metadata to be uploaded. The file must be a non-empty JPEG,
        /// PNG, GIF, or WebP image.</param>
        /// <returns>An HTTP 200 response with the ID of the uploaded image if successful; otherwise, an appropriate error
        /// response indicating the reason for failure.</returns>
        [HttpPost("UploadImage"), Authorize]
        public async Task<ActionResult> UploadImage(ImageUploadDto dto)
        {
            var user = await GetSignedInUserAsync();
            if (user == null) return StatusCode(500);

            if (dto.File == null || dto.File.Length == 0)
                return BadRequest("No file uploaded");

            using var ms = new MemoryStream();
            await dto.File.CopyToAsync(ms);
            var bytes = ms.ToArray();

            // MIME-type check
            var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
            if (!allowedTypes.Contains(dto.File.ContentType))
                return BadRequest("Unsupported image type");

            // Magic-byte check
            if (!ImageHelper.IsImage(bytes))
                return BadRequest("File is not a valid image");

            // Extract name + extension
            var originalName = dto.File.FileName;
            var extension = Path.GetExtension(originalName)
                                .TrimStart('.')
                                .ToLowerInvariant();
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(originalName);

            var entity = new ProfilePicture
            {
                UserId = user.Id,
                PictureType = PictureTypeEnum.InterestPicture,
                Name = dto.Name ?? nameWithoutExtension,
                Type = extension,
                Data = bytes
            };

            await dbContext.ProfilePictures.AddAsync(entity);
            await dbContext.SaveChangesAsync();

            return Ok(new { entity.Id });
        }

        /// <summary>
        /// Retrieves the profile picture with the specified identifier as an image file result.
        /// </summary>
        /// <remarks>The response content type is determined by the stored image type and supports common
        /// formats such as JPEG, PNG, GIF, and WebP. If the image type is unrecognized, the content type defaults to
        /// 'application/octet-stream'.</remarks>
        /// <param name="id">The unique identifier of the profile picture to retrieve.</param>
        /// <returns>An image file containing the profile picture if found; otherwise, a 404 Not Found result.</returns>
        [HttpGet("{id}"), Authorize]
        public async Task<ActionResult> Get(int id)
        {
            var pic = await dbContext.ProfilePictures.FindAsync(id);
            if (pic == null) return NotFound();

            var contentType = pic.Type.ToLowerInvariant() switch
            {
                "jpg" or "jpeg" => "image/jpeg",
                "png" => "image/png",
                "gif" => "image/gif",
                "webp" => "image/webp",
                _ => "application/octet-stream"
            };

            return File(pic.Data, contentType);
        }

        /// <summary>
        /// Retrieves information about a profile picture with the specified identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the profile picture to retrieve.</param>
        /// <returns>An <see cref="ActionResult"/> containing an <see cref="ImageInfoDto"/> with the profile picture information
        /// if found; otherwise, a 404 Not Found result.</returns>
        [HttpGet("{id}/info"), Authorize]
        public async Task<ActionResult> GetInfo(int id)
        {
            var pic = await dbContext.ProfilePictures
                .Where(x => x.Id == id)
                .Select(x => new ImageInfoDto
                {
                    Name = x.Name,
                    Type = x.Type,
                })
                .FirstOrDefaultAsync();

            if (pic == null)
                return NotFound();

            return Ok(pic);
        }

        /// <summary>
        /// Deletes the profile picture with the specified identifier if it belongs to the currently signed-in user.
        /// </summary>
        /// <remarks>This action requires the user to be authenticated. Only the owner of the profile
        /// picture can delete it.</remarks>
        /// <param name="id">The unique identifier of the profile picture to delete.</param>
        /// <returns>An <see cref="NoContentResult"/> if the deletion is successful; <see cref="NotFoundResult"/> if the profile
        /// picture does not exist; <see cref="UnauthorizedResult"/> if the profile picture does not belong to the
        /// current user; or <see cref="StatusCodeResult"/> with status code 500 if the user context cannot be
        /// determined.</returns>
        [HttpDelete("{id}"), Authorize]
        public async Task<ActionResult> Delete(int id)
        {
            var user = await GetSignedInUserAsync();
            if (user == null) return StatusCode(500);

            var pic = await dbContext.ProfilePictures.FindAsync(id);
            if (pic == null || pic.PictureType == PictureTypeEnum.ProfilePicture) return NotFound();
            if (pic.UserId != user.Id) return Unauthorized();

            dbContext.ProfilePictures.Remove(pic);
            await dbContext.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Retrieves a list of interest images associated with a specified user.
        /// </summary>
        /// <remarks>This endpoint requires authentication. If no user ID is provided, the method attempts
        /// to retrieve images for the authenticated user.</remarks>
        /// <param name="userId">The unique identifier of the user whose interest images are to be retrieved. If null or empty, the images
        /// for the currently signed-in user are returned.</param>
        /// <returns>An <see cref="ActionResult{T}"/> containing a list of <see cref="ImageInfoFullDto"/> objects representing
        /// the user's interest images, ordered by upload date descending. Returns a 500 status code if the current user
        /// cannot be determined.</returns>
        [AllowAnonymous] // For testing purposes!
        [HttpGet("InterestImages"), Authorize]
        public async Task<ActionResult<List<ImageInfoFullDto>>> GetUserImages([FromQuery] string? userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                var user = await GetSignedInUserAsync();
                if (user == null) return StatusCode(500);
                userId = user.Id;
            }

            var images = await dbContext.ProfilePictures
                .Where(x => x.UserId == userId && x.PictureType == PictureTypeEnum.InterestPicture)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new ImageInfoFullDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Type = x.Type,
                    UploadedAt = x.CreatedAt
                })
                .ToListAsync();
            return Ok(images);
        }

        // To be moved to a common base controller later
        protected async Task<User?> GetSignedInUserAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return null;
            var user = await dbContext.Users.Where(u => u.Id == userId)
             .Include(u => u.UserRoles)
             .FirstOrDefaultAsync();
            if (user == null) return null;

            return user;
        }
    }
}
