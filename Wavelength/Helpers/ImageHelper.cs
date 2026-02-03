namespace Wavelength.Helpers
{
    /// <summary>
    /// Provides helper methods for working with image data formats.
    /// </summary>
    /// <remarks>The ImageHelper class contains static methods for identifying and handling common image file
    /// formats based on their byte signatures. All members are thread-safe and can be used without creating an instance
    /// of the class.</remarks>
    public static class ImageHelper
    {
        public static bool IsImage(byte[] bytes)
        {
            return IsImage(bytes, options =>
            {
                options.AllowPng = true;
                options.AllowJpg = true;
                options.AllowGif = true;
                options.AllowWebp = true;
            });
        }

        public static bool IsImage(byte[] bytes, Action<ImageValidationOptions> configure)
        {
            var options = new ImageValidationOptions();
            configure(options);

            // PNG
            if (options.AllowPng &&
                bytes.Length > 8 &&
                bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E &&
                bytes[3] == 0x47 && bytes[4] == 0x0D && bytes[5] == 0x0A &&
                bytes[6] == 0x1A && bytes[7] == 0x0A)
                return true;

            // JPEG
            if (options.AllowJpg &&
                bytes.Length > 3 &&
                bytes[0] == 0xFF && bytes[1] == 0xD8 &&
                bytes[^2] == 0xFF && bytes[^1] == 0xD9)
                return true;

            // GIF
            if (options.AllowGif &&
                bytes.Length > 6 &&
                bytes[0] == 0x47 && bytes[1] == 0x49 && bytes[2] == 0x46 &&
                bytes[3] == 0x38 && (bytes[4] == 0x39 || bytes[4] == 0x37) && bytes[5] == 0x61)
                return true;

            // WEBP
            if (options.AllowWebp &&
                bytes.Length > 12 &&
                bytes[0] == 0x52 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x46 &&
                bytes[8] == 0x57 && bytes[9] == 0x45 && bytes[10] == 0x42 && bytes[11] == 0x50)
                return true;

            return false;
        }

        public class ImageValidationOptions
        {
            public bool AllowPng { get; set; }
            public bool AllowJpg { get; set; }
            public bool AllowGif { get; set; }
            public bool AllowWebp { get; set; }
        }
    }
}
