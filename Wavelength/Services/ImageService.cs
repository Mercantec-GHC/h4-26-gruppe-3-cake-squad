using Commons.Enums;
using Commons.Models.Database;
using Commons.Models.Dtos;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using Wavelength.Data;
using Wavelength.Helpers;

namespace Wavelength.Services
{
    /// <summary>
    /// Provides methods for uploading, retrieving, updating, and deleting user profile and interest images, including
    /// avatar management and image metadata operations.
    /// </summary>
    /// <remarks>The ImageService supports image validation, resizing, and format conversion for user profile
    /// images. It enforces file type and size restrictions, and applies image processing such as cropping, resizing,
    /// and blurring as appropriate. Avatar and interest images are managed separately, with avatars subject to
    /// additional processing and visibility rules. All operations are asynchronous and require a valid database
    /// context. Thread safety is not guaranteed; callers should not share instances across threads without proper
    /// synchronization.</remarks>
	public class ImageService
	{
        private readonly AppDbContext dbContext;

        /// <summary>
        /// Initializes a new instance of the ImageService class using the specified database context.
        /// </summary>
        /// <param name="dbContext">The database context to be used for data access operations. Cannot be null.</param>
        public ImageService(AppDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        /// <summary>
        /// Uploads an interest image for the specified user using the provided image data.
        /// </summary>
        /// <remarks>Only images of type JPEG, PNG, GIF, or WebP are supported. The uploaded file must not
        /// exceed 10 MB in size. The method performs validation to ensure the file is a valid image before
        /// saving.</remarks>
        /// <param name="user">The user for whom the image will be uploaded. Cannot be null.</param>
        /// <param name="dto">The data transfer object containing the image file and related metadata. Must specify a supported picture
        /// type and include a valid image file.</param>
        /// <returns>A task that represents the asynchronous upload operation.</returns>
        /// <exception cref="ArgumentException">Thrown if the picture type is not supported, if no file is uploaded, if the file size exceeds 10 MB, if the
        /// file is not a valid image, or if the image type is not supported.</exception>
        public async Task UploadImage(User user, ImageUploadDto dto)
        {
            // Validate picture type
            if (dto.GetPictureType() != PictureTypeEnum.Interest)
                throw new ArgumentException("Unsupported picture type");

            // File presence check
            if (dto.File == null || dto.File.Length == 0)
                throw new ArgumentException("No file uploaded");

            // File size check
            const long maxSize = 10 * 1024 * 1024;
            if (dto.File.Length > maxSize)
                //return BadRequest("File size exceeds 10 MB limit");
                throw new ArgumentException("File size exceeds 10 MB limit");

            // Read file into byte array
            using var ms = new MemoryStream();
            await dto.File.CopyToAsync(ms);
            var bytes = ms.ToArray();

            // MIME-type check
            var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
            if (!allowedTypes.Contains(dto.File.ContentType))
                throw new ArgumentException("Unsupported image type");

            // Magic-byte check
            if (!ImageHelper.IsImage(bytes))
                throw new ArgumentException("File is not a valid image");

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
            await dbContext.ProfilePictures.AddAsync(entity);
            await dbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Uploads and sets the user's avatar image, replacing any existing avatar.
        /// </summary>
        /// <remarks>The uploaded image is cropped to a centered square and resized to 512x512 pixels
        /// before being saved as a PNG. Any existing avatar for the user will be replaced.</remarks>
        /// <param name="user">The user whose avatar will be updated. Must not be null.</param>
        /// <param name="file">The image file to upload as the avatar. Must be a non-empty JPEG or PNG file not exceeding 10 MB in size.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">Thrown if the file is null, empty, exceeds the 10 MB size limit, is not a supported image type (JPEG or
        /// PNG), or is not a valid image file.</exception>
        public async Task UploadAvatar(User user, IFormFile file)
        {
            // File presence check
            if (file == null || file.Length == 0)
                //return BadRequest("No file uploaded");
                throw new ArgumentException("No file uploaded");

            // File size check
            const long maxSize = 10 * 1024 * 1024;
            if (file.Length > maxSize)
                //return BadRequest("File size exceeds 10 MB limit");
                throw new ArgumentException("File size exceeds 10 MB limit");

            // Read file into byte array
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var bytes = ms.ToArray();

            // MIME-type check
            var allowedTypes = new[] { "image/jpeg", "image/png" };
            if (!allowedTypes.Contains(file.ContentType))
                throw new ArgumentException("Unsupported image type");

            // Magic-byte check
            if (!ImageHelper.IsImage(bytes, options =>
            {
                options.AllowJpg = true;
                options.AllowPng = true;
            })) throw new ArgumentException("File is not a valid image");

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
            var entity = await dbContext.ProfilePictures
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
                await dbContext.ProfilePictures.AddAsync(entity);
            }
            entity.Data = finalBytes;
            await dbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Retrieves the avatar image for the specified user, applying a blur effect if the avatar is not visible to
        /// the requesting user.
        /// </summary>
        /// <remarks>An avatar is considered visible if the requesting user is viewing their own avatar or
        /// has visibility permissions for the target user. If the avatar is not visible, a Gaussian blur is applied to
        /// the image before it is returned.</remarks>
        /// <param name="user">The user making the request. Cannot be null.</param>
        /// <param name="userId">The identifier of the user whose avatar is requested. If null or empty, the avatar of the requesting user is
        /// returned.</param>
        /// <returns>A tuple containing the avatar image as a byte array and the MIME type string ("image/png"). The image is
        /// blurred if the avatar is not visible to the requesting user.</returns>
        /// <exception cref="FileNotFoundException">Thrown if the specified user's avatar image cannot be found.</exception>
        public async Task<(byte[], string)> GetAvatarImage(User user, string? userId)
        {
            bool isVisible = false;

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
            var pic = await dbContext.ProfilePictures
                    .Where(x => x.UserId == userId && x.PictureType == PictureTypeEnum.Avatar)
                    .FirstOrDefaultAsync();
            if (pic == null)
                throw new FileNotFoundException("Avatar not found");

            // Load image
            using var image = Image.Load(pic.Data);

            // Apply blur if not visible
            if (!isVisible)
                image.Mutate(x => x.GaussianBlur(25));

            // Save as PNG
            var encoder = new PngEncoder();

            // Convert image to byte array
            using var outStream = new MemoryStream();
            image.Save(outStream, encoder);
            var finalBytes = outStream.ToArray();

            return (finalBytes, "image/png");
        }

        /// <summary>
        /// Deletes the avatar image associated with the specified user.
        /// </summary>
        /// <param name="user">The user whose avatar image is to be deleted. Cannot be null.</param>
        /// <returns>A task that represents the asynchronous delete operation.</returns>
        /// <exception cref="FileNotFoundException">Thrown if the user does not have an avatar image to delete.</exception>
        public async Task DeleteAvatar(User user)
        {
            // Find avatar
            var pic = await dbContext.ProfilePictures
                .Where(x => x.UserId == user.Id && x.PictureType == PictureTypeEnum.Avatar)
                .FirstOrDefaultAsync();

            if (pic == null)
                throw new FileNotFoundException("Avatar not found");

            dbContext.ProfilePictures.Remove(pic);
            await dbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Retrieves the profile image associated with the specified identifier, optionally returning a resized
        /// miniature version.
        /// </summary>
        /// <param name="id">The unique identifier of the profile image to retrieve.</param>
        /// <param name="miniature">If <see langword="true"/>, returns a resized miniature version of the image with a maximum height of 256
        /// pixels; otherwise, returns the original image size. If <see langword="null"/>, the original image size is
        /// returned.</param>
        /// <returns>A tuple containing the image data as a byte array and the corresponding MIME content type string.</returns>
        /// <exception cref="FileNotFoundException">Thrown if an image with the specified <paramref name="id"/> does not exist.</exception>
        public async Task<(byte[], string)> GetInterestImage(int id, bool? miniature = null)
        {
            // Retrieve profile picture from database
            var pic = await dbContext.ProfilePictures.FindAsync(id);
            if (pic == null)
                throw new FileNotFoundException("Image not found");

            // Load image
            using var image = Image.Load(pic.Data);

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

            return (ms.ToArray(), contentType);
        }

        /// <summary>
        /// Retrieves information about a profile picture with the specified identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the profile picture to retrieve.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains an <see cref="ImageInfoDto"/>
        /// with the image's information.</returns>
        /// <exception cref="FileNotFoundException">Thrown if an image with the specified <paramref name="id"/> does not exist.</exception>
        public async Task<ImageInfoDto> GetImageInfo(int id)
        {
            // Retrieve profile picture information from database
            var picture = await dbContext.ProfilePictures
                .Where(x => x.Id == id)
                .Select(x => new ImageInfoDto
                {
                    Name = x.Name,
                    Type = x.Type,
                })
                .FirstOrDefaultAsync();

            if (picture == null)
                throw new FileNotFoundException("Image not found");

            return picture;
        }

        /// <summary>
        /// Deletes a user-owned profile image with the specified identifier.
        /// </summary>
        /// <param name="user">The user requesting the deletion. The user must own the image to delete it.</param>
        /// <param name="id">The unique identifier of the profile image to delete.</param>
        /// <returns>A task that represents the asynchronous delete operation.</returns>
        /// <exception cref="FileNotFoundException">Thrown if the image does not exist or is an avatar image, which cannot be deleted.</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown if the specified image does not belong to the provided user.</exception>
        public async Task DeleteImage(User user, int id)
        {
            // Find profile picture
            var pic = await dbContext.ProfilePictures.FindAsync(id);
            if (pic == null || pic.PictureType == PictureTypeEnum.Avatar)
                throw new FileNotFoundException("Image not found");
            if (pic.UserId != user.Id)
                throw new UnauthorizedAccessException("You do not have permission to delete this image");

            dbContext.ProfilePictures.Remove(pic);
            await dbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Renames an existing profile image for the specified user.
        /// </summary>
        /// <param name="user">The user requesting the image rename operation. Must be the owner of the image.</param>
        /// <param name="dto">An object containing the image identification and the new name to assign to the image.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <exception cref="FileNotFoundException">Thrown if the specified image does not exist or is an avatar image.</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown if the user does not have permission to rename the specified image.</exception>
        public async Task RenameImage(User user, ImageRenameDto dto)
        {
            // Find profile picture
            var pic = await dbContext.ProfilePictures.FindAsync(dto);
            if (pic == null || pic.PictureType == PictureTypeEnum.Avatar)
                throw new FileNotFoundException("Image not found");

            if (pic.UserId != user.Id)
                throw new UnauthorizedAccessException("You do not have permission to rename this image");

            // Update name
            pic.Name = dto.NewName;

            await dbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Retrieves a list of interest images associated with a specified user.
        /// </summary>
        /// <param name="user">The current signed-in user context. Cannot be null.</param>
        /// <param name="userId">The unique identifier of the user whose images to retrieve. If null or empty, images for the signed-in user
        /// are returned.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of image information
        /// objects for the specified user. The list is empty if the user has no interest images.</returns>
        public async Task<List<ImageInfoFullDto>> GetUserImages(User user, string? userId)
        {
            // If no userId provided, use signed-in user
            if (string.IsNullOrEmpty(userId)) userId = user.Id;

            // Retrieve images and map to DTOs
            var images = await dbContext.ProfilePictures
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

            return images;
        }
    }
}
