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
        /// <summary>
        /// Determines whether the specified byte array represents a supported image format.
        /// </summary>
        /// <remarks>This method checks for common image file signatures, including JPEG, PNG, GIF, and
        /// WEBP formats. It does not perform a full validation of the image content and may return false for valid
        /// images in unsupported formats.</remarks>
        /// <param name="bytes">The byte array to examine for image format signatures. Cannot be null.</param>
        /// <returns>true if the byte array matches the signature of a supported image format; otherwise, false.</returns>
        public static bool IsImage(byte[] bytes)
        {
            // JPEG
            if (bytes.Length > 3 &&
                bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[^2] == 0xFF && bytes[^1] == 0xD9)
                return true;

            // PNG
            if (bytes.Length > 8 &&
                bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E &&
                bytes[3] == 0x47 && bytes[4] == 0x0D && bytes[5] == 0x0A &&
                bytes[6] == 0x1A && bytes[7] == 0x0A)
                return true;

            // GIF
            if (bytes.Length > 6 &&
                bytes[0] == 0x47 && bytes[1] == 0x49 && bytes[2] == 0x46 &&
                bytes[3] == 0x38 && (bytes[4] == 0x39 || bytes[4] == 0x37) && bytes[5] == 0x61)
                return true;

            // WEBP (RIFF container)
            if (bytes.Length > 12 &&
                bytes[0] == 0x52 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x46 &&
                bytes[8] == 0x57 && bytes[9] == 0x45 && bytes[10] == 0x42 && bytes[11] == 0x50)
                return true;

            return false;
        }
    }
}
