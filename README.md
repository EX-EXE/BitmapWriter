[![NuGet version](https://badge.fury.io/nu/BitmapWriter.svg)](https://badge.fury.io/nu/BitmapWriter)
# BitmapWriter
Generates a bitmap image file.

## How To Use
### Install by nuget
PM> Install-Package [BitmapWriter](https://www.nuget.org/packages/BitmapWriter/)


## SampleCode
```csharp
var writeBmpFilePath = "<SavePath>";
var width = 1920;
var height = 1080;
var writer = new BitmapWriter(width, height);
foreach (var y in Enumerable.Range(0, height))
{
    foreach (var x in Enumerable.Range(0, width))
    {
        var red = 0;
        var green = 100;
        var blue = 200;
        writer.SetPixel(x, y, (byte)red, (byte)green, (byte)blue);
    }
}
writer.SaveColorImage(writeBmpFilePath);
```
