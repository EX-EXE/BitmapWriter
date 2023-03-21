using System.Reflection;

namespace BitmapWriter.Test
{
    public class WriteTest
    {
        private readonly string writeDir = string.Empty;

        public WriteTest()
        {
            writeDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), nameof(WriteTest));
        }


        [Theory]
        [InlineData(4096, 2160)]
        [InlineData(1920, 1080)]
        [InlineData(100, 50)]
        [InlineData(99, 99)]
        public void SaveFile(int width,int height)
        {
            var targetFrameworkAttribute = Assembly.GetExecutingAssembly()
                .GetCustomAttributes(typeof(System.Runtime.Versioning.TargetFrameworkAttribute), false)
                .Select(x => x as System.Runtime.Versioning.TargetFrameworkAttribute)
                .Where(x => x != null)
                .First();
            var writeFile = System.IO.Path.Combine(writeDir,$"{nameof(SaveFile)}_{width}_{height}.{targetFrameworkAttribute!.FrameworkDisplayName}.bmp");

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
    }
}