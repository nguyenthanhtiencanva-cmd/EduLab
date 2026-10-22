using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace EduLab.Shared.Utils
{
    public static class ScreenHelper
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        private const int SM_CXSCREEN = 0;
        private const int SM_CYSCREEN = 1;

        /// <summary>
        /// Chụp màn hình và nén thành byte array JPEG kích thước thu nhỏ (Thumbnail) tối ưu cho Wi-Fi
        /// </summary>
        /// <param name="targetWidth">Chiều rộng ảnh thu nhỏ (mặc định 360px)</param>
        /// <param name="targetHeight">Chiều cao ảnh thu nhỏ (mặc định 202px - tỷ lệ 16:9)</param>
        /// <param name="quality">Mức chất lượng JPEG (1-100, 45 là mức tối ưu sắc nét mà siêu nhẹ)</param>
        public static byte[] CaptureThumbnail(int targetWidth = 360, int targetHeight = 202, long quality = 45)
        {
            try
            {
                // Lấy kích thước màn hình chính qua Win32 API
                int screenWidth = GetSystemMetrics(SM_CXSCREEN);
                int screenHeight = GetSystemMetrics(SM_CYSCREEN);

                if (screenWidth <= 0) screenWidth = 1920;
                if (screenHeight <= 0) screenHeight = 1080;

                using var fullBmp = new Bitmap(screenWidth, screenHeight, PixelFormat.Format24bppRgb);
                using (var g = Graphics.FromImage(fullBmp))
                {
                    g.CopyFromScreen(0, 0, 0, 0, new Size(screenWidth, screenHeight), CopyPixelOperation.SourceCopy);
                }

                // Resize xuống Thumbnail
                using var thumbBmp = new Bitmap(targetWidth, targetHeight, PixelFormat.Format24bppRgb);
                using (var gThumb = Graphics.FromImage(thumbBmp))
                {
                    gThumb.InterpolationMode = InterpolationMode.Bilinear;
                    gThumb.SmoothingMode = SmoothingMode.HighSpeed;
                    gThumb.PixelOffsetMode = PixelOffsetMode.HighSpeed;
                    gThumb.DrawImage(fullBmp, 0, 0, targetWidth, targetHeight);
                }

                // Nén JPEG
                using var ms = new MemoryStream();
                var encoder = GetEncoder(ImageFormat.Jpeg);
                if (encoder != null)
                {
                    using var encoderParams = new EncoderParameters(1);
                    encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, quality);
                    thumbBmp.Save(ms, encoder, encoderParams);
                }
                else
                {
                    thumbBmp.Save(ms, ImageFormat.Jpeg);
                }

                return ms.ToArray();
            }
            catch
            {
                return Array.Empty<byte>();
            }
        }

        private static ImageCodecInfo? GetEncoder(ImageFormat format)
        {
            var codecs = ImageCodecInfo.GetImageEncoders();
            foreach (var codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }
    }
}
