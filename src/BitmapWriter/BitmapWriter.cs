using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace BitmapWriter;

public class BitmapWriter : IDisposable
{
    private BitmapFileHeader fileHeader = new BitmapFileHeader();
    private BitmapInfoHeader infoHeader = new BitmapInfoHeader();
    private byte[] bufferImage = Array.Empty<byte>();

    private static void DeleteExistsFile(string path)
    {
        if (System.IO.File.Exists(path))
        {
            System.IO.File.Delete(path);
        }
    }

    private static Span<byte> ToSpan<T>(ref T data)
    {
        unsafe
        {
            return new Span<byte>(Unsafe.AsPointer(ref data), Marshal.SizeOf(typeof(T)));
        }
    }

    public BitmapWriter(int width, int height)
    {
        infoHeader.Width = width;
        infoHeader.Height = height;
        bufferImage = ArrayPool<byte>.Shared.Rent(width * height * 3);
    }

    #region IDisposable
    private bool disposedValue;
    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                ArrayPool<byte>.Shared.Return(bufferImage);
            }
            bufferImage = Array.Empty<byte>();
            disposedValue = true;
        }
    }
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
    #endregion


    public (byte red, byte green, byte blue) GetPixel(int x, int y)
    {
        if (infoHeader.Width <= x)
        {
            throw new ArgumentException($"Overflow x.({x}) : Width({infoHeader.Width})");
        }
        if (infoHeader.Height <= y)
        {
            throw new ArgumentException($"Overflow y.({y}) : Height({infoHeader.Height})");
        }
        var x_pos = x * 3;
        var widthSize = infoHeader.Width * 3;

        var green = bufferImage[x_pos + 0 + y * widthSize];
        var blue = bufferImage[x_pos + 1 + y * widthSize];
        var red = bufferImage[x_pos + 2 + y * widthSize];
        return (red, green, blue);
    }

    public void SetPixel(int x, int y, byte red, byte green, byte blue)
    {
        if (infoHeader.Width <= x)
        {
            throw new ArgumentException($"Overflow x.({x}) : Width({infoHeader.Width})");
        }
        if (infoHeader.Height <= y)
        {
            throw new ArgumentException($"Overflow y.({y}) : Height({infoHeader.Height})");
        }
        var x_pos = x * 3;
        var widthSize = infoHeader.Width * 3;
        bufferImage[x_pos + 0 + y * widthSize] = green;
        bufferImage[x_pos + 1 + y * widthSize] = blue;
        bufferImage[x_pos + 2 + y * widthSize] = red;
    }


    public void SaveColorImage(string path)
    {
        // Data
        var fileHeaderSpan = ToSpan(ref fileHeader);
        var infoHeaderSpan = ToSpan(ref infoHeader);
        var srcImageSpan = bufferImage.AsSpan();

        var srcImageWidthSize = infoHeader.Width * 3;
        var dstPaddingWidthSize = 4 - srcImageWidthSize % 4;
        var dstImageTotalSize = (srcImageWidthSize + dstPaddingWidthSize) * (uint)infoHeader.Height;

        fileHeader.Size = (uint)fileHeaderSpan.Length + (uint)infoHeaderSpan.Length + (uint)dstImageTotalSize;
        fileHeader.OffBits = (uint)fileHeaderSpan.Length + (uint)infoHeaderSpan.Length;
        infoHeader.SizeImage = (uint)dstImageTotalSize;
        infoHeader.BitCount = (ushort)BitmapColorBit.Bit24;
        infoHeader.Compression = (uint)BitmapCompression.Rgb;

        // Save
        DeleteExistsFile(path);
        using var fileStream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite, 4096, true);
        fileStream.Write(fileHeaderSpan);
        fileStream.Write(infoHeaderSpan);
        // ImageData
        var dstPaddingBytes = new byte[dstPaddingWidthSize];
        for (var heightIndex = infoHeader.Height - 1; 0 <= heightIndex; --heightIndex)
        {
            var lineSpan = srcImageSpan.Slice(srcImageWidthSize * heightIndex, srcImageWidthSize);
            if (dstPaddingWidthSize == 0)
            {
                fileStream.Write(lineSpan);
            }
            else
            {
                fileStream.Write(lineSpan);
                fileStream.Write(dstPaddingBytes);
            }
        }
    }

}

#if NETSTANDARD2_0
internal static class FileStreamExtensions
{
    public static void Write(this FileStream fileStream, ReadOnlySpan<byte> buffer) 
    {
        fileStream.Write(buffer.ToArray(), 0, buffer.Length);
    }
}
#endif
