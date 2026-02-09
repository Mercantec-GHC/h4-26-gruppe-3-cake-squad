using Commons.Enums;
using Commons.Models.Database;
using Commons.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
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
    public class ImagesController : BaseController
    {
        /// <summary>
        /// Initializes a new instance of the ImagesController class using the specified database context.
        /// </summary>
        /// <param name="dbContext">The database context to be used by the controller for data access operations. Cannot be null.</param>
        public ImagesController(AppDbContext dbContext) : base(dbContext) {}

        /// <summary>
        /// Uploads a new interest image for the currently authenticated user.
        /// </summary>
        /// <remarks>This endpoint requires authentication. Only images of type Interest are accepted. The
        /// uploaded file is validated for type, size, and content to ensure it is a supported and valid image
        /// format.</remarks>
        /// <param name="dto">An object containing the image file and related metadata to be uploaded. The file must be a valid image of
        /// type JPEG, PNG, GIF, or WebP, and its size must not exceed 10 MB. The picture type must be set to Interest.</param>
        /// <returns>An HTTP 200 OK result containing the ID of the newly uploaded image if successful; otherwise, an appropriate
        /// error response indicating the reason for failure.</returns>
        [HttpPost("Upload"), Authorize]
        public async Task<ActionResult> UploadImage(ImageUploadDto dto)
        {
            // Validate picture type
            if (dto.GetPictureType() != PictureTypeEnum.Interest)
                return BadRequest("Invalid picture type for this endpoint");

            // Validate user
            var user = await GetSignedInUserAsync();
            if (user == null) return StatusCode(500);

            // File presence check
            if (dto.File == null || dto.File.Length == 0)
                return BadRequest("No file uploaded");

            // File size check
            const long maxSize = 10 * 1024 * 1024;
            if (dto.File.Length > maxSize)
                return BadRequest("File size exceeds 10 MB limit");

            // Read file into byte array
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

            // Create and save entity
            var entity = new ProfilePicture
            {
                UserId = user.Id,
                PictureType = PictureTypeEnum.Interest,
                Name = dto.Name ?? nameWithoutExtension,
                Type = extension,
                Data = bytes
            };
            await DbContext.ProfilePictures.AddAsync(entity);
            await DbContext.SaveChangesAsync();

            return Ok(new { entity.Id });
        }

        /// <summary>
        /// Uploads and sets the authenticated user's avatar image. The uploaded image is validated, cropped to a
        /// centered square, resized to 512x512 pixels, and stored as a PNG.
        /// </summary>
        /// <remarks>Only authenticated users can upload an avatar. The uploaded image is validated for
        /// file type and content to ensure it is a valid JPEG or PNG image. Existing avatars are replaced when a new
        /// image is uploaded.</remarks>
        /// <param name="file">The image file to upload as the user's avatar. Must be a non-empty JPEG or PNG file not exceeding 10 MB in
        /// size.</param>
        /// <returns>A 204 No Content response if the avatar is uploaded successfully; otherwise, a 400 Bad Request response if
        /// the file is invalid or a 500 Internal Server Error if the user cannot be determined.</returns>
        [HttpPost("Avatar/Upload"), Authorize]
        public async Task<ActionResult> UploadAvatar(IFormFile file)
        {
            // File presence check
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded");

            // File size check
            const long maxSize = 10 * 1024 * 1024;
            if (file.Length > maxSize)
                return BadRequest("File size exceeds 10 MB limit");

            // Validate user
            var user = await GetSignedInUserAsync();
            if (user == null) return StatusCode(500);

            // Read file into byte array
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var bytes = ms.ToArray();

            // MIME-type check
            var allowedTypes = new[] { "image/jpeg", "image/png" };
            if (!allowedTypes.Contains(file.ContentType))
                return BadRequest("Unsupported image type");

            // Magic-byte check
            if (!ImageHelper.IsImage(bytes, options =>
            {
                options.AllowJpg = true;
                options.AllowPng = true;
            })) return BadRequest("File is not a valid image");

            // Load image
            using var image = Image.Load(bytes);

            //  Crop to centered square
            int size = Math.Min(image.Width, image.Height);
            int x = (image.Width - size) / 2;
            int y = (image.Height - size) / 2;

            // Define crop rectangle
            var cropRect = new Rectangle(x, y, size, size);

            // Perform crop and resize
            image.Mutate(m => m.Crop(cropRect)
                .Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Stretch,
                    Size = new Size(512, 512)
                })
            );

            // Save as PNG
            var encoder = new PngEncoder();

            // Convert image to byte array
            using var outStream = new MemoryStream();
            image.Save(outStream, encoder);
            var finalBytes = outStream.ToArray();

            // Check if user already has an avatar
            var entity = await DbContext.ProfilePictures
                .FirstOrDefaultAsync(x => x.UserId == user.Id && x.PictureType == PictureTypeEnum.Avatar);

            // If not, create new
            if (entity == null)
            {
                entity = new ProfilePicture
                {
                    UserId = user.Id,
                    PictureType = PictureTypeEnum.Avatar,
                    Name = "avatar",
                    Type = "png"
                };
                await DbContext.ProfilePictures.AddAsync(entity);
            }
            entity.Data = finalBytes;
            await DbContext.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Returns the avatar image for the specified user as a PNG file, applying a blur effect if the avatar is not
        /// visible to the current user.
        /// </summary>
        /// <remarks>The visibility of the requested avatar depends on the relationship between the
        /// signed-in user and the target user. If the avatar is not visible, a blurred version of the image is
        /// returned. This endpoint requires authentication.</remarks>
        /// <param name="userId">The unique identifier of the user whose avatar is requested. If null or empty, the avatar of the currently
        /// signed-in user is returned.</param>
        /// <returns>An <see cref="ActionResult"/> containing the PNG image of the user's avatar. Returns a blurred image if the
        /// avatar is not visible to the current user, a 404 Not Found result if the avatar does not exist, or a 500
        /// Internal Server Error if the current user cannot be determined.</returns>
        [Authorize]
        [HttpGet("Avatar")]
        [HttpGet("Avatar/{userId}")]
        public async Task<ActionResult> Avatar(string? userId)
        {
            bool isVisible = false;

            // Determine visibility
            var user = await GetSignedInUserAsync(q => q.Include(u => u.UserVisibilities));
            if (user == null) return StatusCode(500);

            if (string.IsNullOrEmpty(userId))
            {
                // If no userId provided, show own avatar
                userId = user.Id;
                isVisible = true;
            }
            else if (user != null && user.Id == userId)
            {
                // If requesting own avatar
                isVisible = true;
            }
            else
            {
                // Check user visibility
                isVisible = user!.UserVisibilities.Any(uv => uv.TargetUserId == userId && uv.Visibility == UserVisibilityEnum.Visible);
			}

            // Find the user's avatar
            var pic = await DbContext.ProfilePictures
                    .Where(x => x.UserId == userId && x.PictureType == PictureTypeEnum.Avatar)
                    .FirstOrDefaultAsync();
            if (pic == null) return NotFound();

            // Load image
            using var image = SixLabors.ImageSharp.Image.Load(pic.Data);

            // Apply blur if not visible
            if (!isVisible)
                image.Mutate(x => x.GaussianBlur(25));

            // Save as PNG
            var encoder = new PngEncoder();

            // Convert image to byte array
            using var outStream = new MemoryStream();
            image.Save(outStream, encoder);
            var finalBytes = outStream.ToArray();

            return File(finalBytes, "image/png");
        }

        /// <summary>
        /// Deletes the currently signed-in user's avatar image.
        /// </summary>
        /// <remarks>This action requires the user to be authenticated. Only the avatar associated with
        /// the currently signed-in user is affected.</remarks>
        /// <returns>An <see cref="ActionResult"/> indicating the result of the operation. Returns <see
        /// cref="Microsoft.AspNetCore.Mvc.NoContentResult"/> if the avatar was successfully deleted; <see
        /// cref="Microsoft.AspNetCore.Mvc.NotFoundResult"/> if no avatar exists for the user; or <see
        /// cref="Microsoft.AspNetCore.Mvc.StatusCodeResult"/> with status code 500 if the user could not be determined.</returns>
        [HttpDelete("Avatar"), Authorize]
        public async Task<ActionResult> DeleteAvatar()
        {
            // Validate user
            var user = await GetSignedInUserAsync();
            if (user == null) return StatusCode(500);

            // Find avatar
            var pic = await DbContext.ProfilePictures
                .Where(x => x.UserId == user.Id && x.PictureType == PictureTypeEnum.Avatar)
                .FirstOrDefaultAsync();

            if (pic == null) return NotFound();

            DbContext.ProfilePictures.Remove(pic);
            await DbContext.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Retrieves the profile picture with the specified identifier, optionally returning a resized miniature
        /// version.
        /// </summary>
        /// <param name="id">The unique identifier of the profile picture to retrieve.</param>
        /// <param name="miniature">If set to <see langword="true"/>, returns a resized miniature version of the image with a maximum height of
        /// 256 pixels. If <see langword="false"/> or <see langword="null"/>, returns the original image size.</param>
        /// <returns>An <see cref="ActionResult"/> containing the image file in its original or miniature size and format.
        /// Returns a 404 Not Found response if the specified profile picture does not exist.</returns>
        [HttpGet("Interest/{id}"), Authorize]
        public async Task<ActionResult> Get(int id, bool? miniature = null)
        {
            // Retrieve profile picture from database
            var pic = await DbContext.ProfilePictures.FindAsync(id);
            if (pic == null) return NotFound();

            // Load image
            using var image = SixLabors.ImageSharp.Image.Load(pic.Data);

            // Resize if miniature requested
            if (miniature == true)
            {
                var resizeOptions = new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(0, 256)
                };

                image.Mutate(x => x.Resize(resizeOptions));
            }

            // Convert image to byte array
            using var ms = new MemoryStream();
            IImageEncoder encoder = pic.Type.ToLowerInvariant() switch
            {
                "jpg" or "jpeg" => new JpegEncoder(),
                "png" => new PngEncoder(),
                "gif" => new GifEncoder(),
                "webp" => new WebpEncoder(),
                _ => new PngEncoder()
            };
            image.Save(ms, encoder);

            // Prepare file response
            var fileName = $"{pic.Name}.{pic.Type}";
            var contentType = $"image/{pic.Type}";

            return File(ms.ToArray(), contentType);
        }

        /// <summary>
        /// Retrieves information about a profile picture with the specified identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the profile picture to retrieve.</param>
        /// <returns>An <see cref="ActionResult"/> containing an <see cref="ImageInfoDto"/> with the profile picture information
        /// if found; otherwise, a 404 Not Found result.</returns>
        [HttpGet("Interest/{id}/info"), Authorize]
        public async Task<ActionResult> GetInfo(int id)
        {
            // Retrieve profile picture information from database
            var pic = await DbContext.ProfilePictures
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
        [HttpDelete("Interest/{id}"), Authorize]
        public async Task<ActionResult> Delete(int id)
        {
            // Validate user
            var user = await GetSignedInUserAsync();
            if (user == null) return StatusCode(500);

            // Find profile picture
            var pic = await DbContext.ProfilePictures.FindAsync(id);
            if (pic == null || pic.PictureType == PictureTypeEnum.Interest) return NotFound();
            if (pic.UserId != user.Id) return Unauthorized();

            DbContext.ProfilePictures.Remove(pic);
            await DbContext.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Renames an existing profile picture for the signed-in user.
        /// </summary>
        /// <param name="dto">An object containing the identifier of the profile picture to rename and the new name to assign.</param>
        /// <returns>An <see cref="ActionResult"/> indicating the result of the operation. Returns <see cref="NoContentResult"/>
        /// if the rename is successful; <see cref="NotFoundResult"/> if the profile picture does not exist or is not
        /// eligible for renaming; <see cref="UnauthorizedResult"/> if the user does not own the picture; or <see
        /// cref="StatusCodeResult"/> with status code 500 if the user is not signed in.</returns>
        [HttpPut("Interest/{id}/Rename"), Authorize]
        public async Task<ActionResult> Rename(ImageRenameDto dto)
        {
            // Validate user
            var user = await GetSignedInUserAsync();
            if (user == null) return StatusCode(500);

            // Find profile picture
            var pic = await DbContext.ProfilePictures.FindAsync(dto);
            if (pic == null || pic.PictureType == PictureTypeEnum.Interest) return NotFound();
            if (pic.UserId != user.Id) return Unauthorized();

            // Update name
            pic.Name = dto.NewName;

            await DbContext.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Retrieves a list of interest images associated with the specified user.
        /// </summary>
        /// <remarks>Requires authentication. The images are ordered by upload date in descending order,
        /// with the most recently uploaded images first.</remarks>
        /// <param name="userId">The unique identifier of the user whose interest images are to be retrieved. If null or empty, retrieves
        /// images for the currently signed-in user.</param>
        /// <returns>An <see cref="ActionResult{T}"/> containing a list of <see cref="ImageInfoFullDto"/> objects representing
        /// the user's interest images. Returns an empty list if no images are found.</returns>
        [Authorize]
        [HttpGet("Interest/All")]
        [HttpGet("Interest/All/{userId}")]
        public async Task<ActionResult<List<ImageInfoFullDto>>> GetUserImages(string? userId)
        {
            // Validate user
            var user = await GetSignedInUserAsync();
            if (user == null) return StatusCode(500);

            // If no userId provided, use signed-in user
            if (string.IsNullOrEmpty(userId)) userId = user.Id;

            // Retrieve images and map to DTOs
            var images = await DbContext.ProfilePictures
                .Where(x => x.UserId == userId && x.PictureType == PictureTypeEnum.Interest)
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
    }
}
