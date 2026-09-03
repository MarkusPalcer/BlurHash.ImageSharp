using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace Blurhash.ImageSharp.Test
{
    public class BlurhasherTests
    {
        const string SourceHash = "|HFFaXYk^6#M9vF~W@j=#*:d5b,1J5PBV=R:s;w[@[or[k6oO[TLtJrqnO};Fxi^OZE3NgM}sps,jMFxS#OtcXnzRjxZxHj]OYNeWGJCs9xunhwIXBIpNaxHNGr;v}aeo0XmxZXS$et6#*$ft6nhxHnNV@w{nOaKwfNHo0";

        [Fact]
        public async Task DecodingTests()
        {
            var result = Blurhasher.Decode(SourceHash, 300, 200);

            await using var ms = new MemoryStream();
            await result.SaveAsPngAsync("output.png");
        }

        [Fact]
        public async Task EncodingTests()
        {
            var sourceImage = await Image.LoadAsync<Rgba32>(Path.Combine("Resources", "Specimens", "Sample.jpg"));

            var result = Blurhasher.Encode(sourceImage, 9, 9);

            result.Should().Be(SourceHash);
        }
    }
}
