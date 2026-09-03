using System;
using System.Runtime.InteropServices;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Blurhash.ImageSharp
{
    public static class Blurhasher
    {
        /// <summary>
        /// Encodes a picture into a Blurhash string
        /// </summary>
        /// <param name="image">The picture to encode</param>
        /// <param name="componentsX">The number of components used on the X-Axis for the DCT</param>
        /// <param name="componentsY">The number of components used on the Y-Axis for the DCT</param>
        /// <param name="progress">An optional progress reporter</param>
        /// <returns>The resulting Blurhash string</returns>
        public static string Encode(Image<Rgb24> image, int componentsX, int componentsY, IProgress<int>? progress = null)
        {
            return EncodeInternal(image, componentsX, componentsY, progress);
        }

        /// <summary>
        /// Encodes a picture into a Blurhash string
        /// </summary>
        /// <param name="image">The picture to encode</param>
        /// <param name="componentsX">The number of components used on the X-Axis for the DCT</param>
        /// <param name="componentsY">The number of components used on the Y-Axis for the DCT</param>
        /// <param name="progress">An optional progress reporter</param>
        /// <returns>The resulting Blurhash string</returns>
        public static string Encode(Image<Rgba32> image, int componentsX, int componentsY, IProgress<int>? progress = null)
        {
            return EncodeInternal(image, componentsX, componentsY, progress);
        }

        /// <summary>
        /// Decodes a Blurhash string into a <c>SixLabors.ImageSharp.Image</c>
        /// </summary>
        /// <param name="blurhash">The blurhash string to decode</param>
        /// <param name="outputWidth">The desired width of the output in pixels</param>
        /// <param name="outputHeight">The desired height of the output in pixels</param>
        /// <param name="punch">A value that affects the contrast of the decoded image. 1 means normal, smaller values will make the effect more subtle, and larger values will make it stronger.</param>
        /// /// <param name="progress">An optional progress reporter</param>
        /// <returns>The decoded preview</returns>
        public static Image<Rgb24> Decode(string blurhash, int outputWidth, int outputHeight, double punch = 1.0, IProgress<int>? progress = null)
        {
            var data = new Rgb24[outputWidth * outputHeight];

            var decoder = new StreamedDecoder(blurhash, outputWidth, outputHeight, ChunkProcessor, punch, progress);            
            decoder.Run();
            
            return Image.WrapMemory(Configuration.Default, new Memory<Rgb24>(data), outputWidth, outputHeight);

            void ChunkProcessor(ReadOnlySpan<StreamedPixel> buffer)
            {
                foreach (var source in buffer)
                {
                    ref var dest = ref data[source.X + source.Y * outputWidth];
                    dest.R = (byte)MathUtils.LinearTosRgb(source.Red);
                    dest.G = (byte)MathUtils.LinearTosRgb(source.Green);
                    dest.B = (byte)MathUtils.LinearTosRgb(source.Blue);
                }
            }
        }

        private static string EncodeInternal<T>(Image<T> sourceBitmap,
            int componentsX,
            int componentsY,
            IProgress<int>? progress = null) where T : unmanaged, IPixel<T>
        {
            var encoder = new StreamedEncoder(componentsX, componentsY, sourceBitmap.Width, sourceBitmap.Height, progress);

            if (typeof(T) != typeof(Rgba32) && typeof(T) != typeof(Rgb24))
                throw new ArgumentOutOfRangeException(nameof(sourceBitmap), "Only Rgba32 and Rgb24 are supported");
            
            var width = sourceBitmap.Width;
            var bytesPerPixel = sourceBitmap.PixelType.BitsPerPixel / 8;
            
            sourceBitmap.ProcessPixelRows(pixelAccessor =>
            {
                Span<StreamedPixel> buffer = stackalloc StreamedPixel[pixelAccessor.Width];
                
                for (var y = 0; y < pixelAccessor.Height; y++)
                {
                    var rgbValues = MemoryMarshal.AsBytes(pixelAccessor.GetRowSpan(y));

                    var index = 0;

                    for (var x = 0; x < width; x++)
                    {
                        buffer[x].Red = MathUtils.SRgbToLinear(rgbValues[index]);
                        buffer[x].Green = MathUtils.SRgbToLinear(rgbValues[index + 1]);
                        buffer[x].Blue = MathUtils.SRgbToLinear(rgbValues[index + 2]);
                        buffer[x].X = x;
                        buffer[x].Y = y;
                        index += bytesPerPixel;
                    }
                    
                    encoder.Process(buffer);
                }
            });
            
            return encoder.Finish();
        }
    }
}
