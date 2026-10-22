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
        private static extern IntPtr GetWindowDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll")]
        private static extern bool BitBlt(IntPtr hObject, int nXDest, int nYDest, int nWidth,
            int nHeight, IntPtr hObjectSource, int nXSrc, int nYSrc, int dwRop);

        private const int SRCCOPY = 0x00CC0020;

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
                // Lấy kích thước toàn màn hình chính
                int screenWidth = 1920;
                int screenHeight = 1080;
                
                try
                {
                    // Khi chạy trên Windows Desktop
                    screenWidth = System.Windows.Forms.Screen.PrimaryScreen?.Bounds.Width ?? 1920;
                    screenHeight = System.Windows.Forms.Screen.PrimaryScreen?.Bounds.Height ?? 1080;
                }
                catch
                {
                    // Fallback
                }

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
