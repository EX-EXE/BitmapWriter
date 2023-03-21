using System.Reflection;
using System.Runtime.Versioning;

namespace BitmapWriter.Test
{
    public class WriteTest
    {
        private readonly string frameworkName = string.Empty;
        private readonly string writeDir = string.Empty;

        public WriteTest()
        {
            writeDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), nameof(WriteTest));
#pragma warning disable CS8601 // Possible null reference assignment.
            frameworkName = Assembly.GetExecutingAssembly()
                .GetCustomAttributes(typeof(System.Runtime.Versioning.TargetFrameworkAttribute), false)
                .Select(x => x as System.Runtime.Versioning.TargetFrameworkAttribute)
                .Select(x => x != null ? x.FrameworkDisplayName : "Unknown")
                .First()
                .Replace(".","_").Replace(" ","_");
#pragma warning restore CS8601 // Possible null reference assignment.
        }


        [Theory]
        [InlineData(4096, 2160)]
        [InlineData(1920, 1080)]
        [InlineData(100, 50)]
        [InlineData(99, 99)]
        public void SaveColorImage(int width,int height)
        {
            var writeFile = System.IO.Path.Combine(writeDir,$"{nameof(SaveColorImage)}_{width}_{height}.{frameworkName}.bmp");

            var writer = new BitmapWriter(width, height);
            foreach (var y in Enumerable.Range(0, height))
            {
                foreach (var x in Enumerable.Range(0, width))
                {
                    var red = (double)byte.MaxValue / (double)(width+height) * (double)(x+y);
                    var green = (double)byte.MaxValue / (double)height * (double)y;
                    var blue = (double)byte.MaxValue / (double)width * (double)x;
                    writer.SetPixel(x, y, (byte)red, (byte)green, (byte)blue);
                }
            }
            writer.SaveColorImage(writeFile);
        }

        [Theory]
        [InlineData(4096, 2160)]
        [InlineData(1920, 1080)]
        [InlineData(100, 50)]
        [InlineData(99, 99)]
        public void SaveRgb888Image(int width, int height)
        {
            var writeFile = System.IO.Path.Combine(writeDir, $"{nameof(SaveRgb888Image)}_{width}_{height}.{frameworkName}.bmp");

            var writer = new BitmapWriter(width, height);
            foreach (var y in Enumerable.Range(0, height))
            {
                foreach (var x in Enumerable.Range(0, width))
                {
                    var red = (double)byte.MaxValue / (double)(width + height) * (double)(x + y);
                    var green = (double)byte.MaxValue / (double)height * (double)y;
                    var blue = (double)byte.MaxValue / (double)width * (double)x;
                    writer.SetPixel(x, y, (byte)red, (byte)green, (byte)blue);
                }
            }
            writer.SaveRgb888Image(writeFile);
        }
    }
}
