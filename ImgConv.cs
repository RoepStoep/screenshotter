using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace Screenshotter
{
    public static class ImgConv
    {

        private static ImageCodecInfo _jpegCodec;

        static ImgConv()
        {

            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
            foreach (var codec in codecs)
            {
                if (codec.FormatID == ImageFormat.Jpeg.Guid)
                {
                    _jpegCodec = codec;
                    break;
                }
            }

        }

        public static long JpegQuality = 100L;

        public static Bitmap ScaleBitmap(Bitmap bmp, float targetWidth, float targetHeight)
        {

            float scale = Math.Min(targetWidth / bmp.Width, targetHeight / bmp.Height);
            int scaleWidth = (int)(bmp.Width * scale), scaleHeight = (int)(bmp.Height * scale);

            Bitmap output = new Bitmap(bmp, new Size(scaleWidth, scaleHeight));
            return output;

        }

        public static MemoryStream Image2Jpeg(Image img)
        {

            MemoryStream streamOut = new MemoryStream();

            EncoderParameters parameters = new EncoderParameters(1);
            EncoderParameter parameter = new EncoderParameter(Encoder.Quality, JpegQuality);

            parameters.Param[0] = parameter;
            img.Save(streamOut, _jpegCodec, parameters);

            streamOut.Position = 0;
            return streamOut;

        }

        public static MemoryStream Bitmap2Jpeg(Bitmap bmp)
        {

            MemoryStream streamOut = new MemoryStream();

            EncoderParameters parameters = new EncoderParameters(1);
            EncoderParameter parameter = new EncoderParameter(Encoder.Quality, JpegQuality);

            parameters.Param[0] = parameter;
            bmp.Save(streamOut, _jpegCodec, parameters);

            streamOut.Position = 0;
            return streamOut;

        }

    }
}
