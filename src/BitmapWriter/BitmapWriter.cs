using System.Buffers;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static System.Net.Mime.MediaTypeNames;

namespace BitmapWriter;

public class BitmapWriter : IDisposable
{
    private BitmapFileHeader fileHeader = new BitmapFileHeader();
    private BitmapInfoHeader infoHeader = new BitmapInfoHeader();
    private byte[] bufferImage = Array.Empty<byte>();

    private static void DeleteFile(string path)
    {
        if (System.IO.File.Exists(path))
        {
            System.IO.File.Delete(path);
        }
    }
    private static void CreateDirectory(string path)
    {
        var parentDir = System.IO.Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(parentDir) && !System.IO.Directory.Exists(parentDir))
        {
            System.IO.Directory.CreateDirectory(parentDir);
        }
    }

    private static Span<byte> ToSpan<T>(ref T data)
    {
        unsafe
        {
            return new Span<byte>(Unsafe.AsPointer(ref data), Marshal.SizeOf(typeof(T)));
        }
    }

    private static int GetImageWidthSize(int width, BitmapColorBit colorBit)
    {
        var bitCount = width;
        switch (colorBit)
        {
            case BitmapColorBit.Bit1:
                bitCount *= 1;
                break;
            case BitmapColorBit.Bit4:
                bitCount *= 4;
                break;
            case BitmapColorBit.Bit8:
                bitCount *= 8;
                break;
            case BitmapColorBit.Bit24:
                bitCount *= 24;
                break;
            case BitmapColorBit.Bit32:
                bitCount *= 32;
                break;
            default:
                throw new InvalidOperationException($"Invalid color bit. : {colorBit}");
        }
        // 8 bit
        var bitMod = bitCount % 8;
        var bitTotal = bitCount + (bitMod == 0 ? 0 : 8 - bitMod);
        // 4 byte
        var byteCount = bitTotal / 8;
        var byteMod = byteCount % 4;
        return byteCount + (byteMod == 0 ? 0 : (4 - byteMod));
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

        var blue = bufferImage[x_pos + 0 + y * widthSize];
        var green = bufferImage[x_pos + 1 + y * widthSize];
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
        bufferImage[x_pos + 0 + y * widthSize] = blue;
        bufferImage[x_pos + 1 + y * widthSize] = green;
        bufferImage[x_pos + 2 + y * widthSize] = red;
    }


    public void SaveColorImage(string path)
    {
        // Data
        var fileHeaderSpan = ToSpan(ref fileHeader);
        var infoHeaderSpan = ToSpan(ref infoHeader);
        var srcImageSpan = bufferImage.AsSpan();

        var srcImageWidthSize = infoHeader.Width * 3;
        var dstColorBit = BitmapColorBit.Bit24;
        var dstImageWidth = GetImageWidthSize(infoHeader.Width, dstColorBit);
        var dstPaddingWidthSize = dstImageWidth - srcImageWidthSize;
        var dstImageTotalSize = (srcImageWidthSize + dstPaddingWidthSize) * (uint)infoHeader.Height;

        fileHeader.Size = (uint)fileHeaderSpan.Length + (uint)infoHeaderSpan.Length + (uint)dstImageTotalSize;
        fileHeader.OffBits = (uint)fileHeaderSpan.Length + (uint)infoHeaderSpan.Length;
        infoHeader.SizeImage = (uint)dstImageTotalSize;
        infoHeader.BitCount = (ushort)BitmapColorBit.Bit24;
        infoHeader.Compression = (uint)BitmapCompression.Rgb;

        // Save
        DeleteFile(path);
        CreateDirectory(path);
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


    public void SavePaletteGrayScaleImage(string path, BitmapColorBit colorBit, BitmapCompression compression)
    {
        switch (colorBit)
        {
            case BitmapColorBit.Bit1:
            case BitmapColorBit.Bit4:
            case BitmapColorBit.Bit8:
                break;
            default:
                throw new ArgumentException($"Compression with this bit depth is not possible.");
        }
        switch (compression)
        {
            case BitmapCompression.Rgb:
                break;

            case BitmapCompression.Rle8:
                if (colorBit != BitmapColorBit.Bit8)
                {
                    throw new ArgumentException($"This compression method cannot be used with this bit depth.");
                }
                break;

            case BitmapCompression.Rle4:
                if (colorBit != BitmapColorBit.Bit4)
                {
                    throw new ArgumentException($"This compression method cannot be used with this bit depth.");
                }
                break;

            default:
                throw new ArgumentException($"This compression method cannot be used with this bit depth.");
        }


        var paletteList = new List<BitmapPaletteData>();
        var paletteIndexDict = new Dictionary<int, byte>();
        byte GetOrAddPalett(byte g, byte b, byte r)
        {
            if (paletteList == null || paletteIndexDict == null)
            {
                throw new InvalidOperationException("null data.");
            }

            int num = (r << 16) | (g << 8) | (b << 0);
            if (paletteIndexDict.TryGetValue(num, out var index))
            {
                return index;
            }

            var data = new BitmapPaletteData(r, g, b);
            var addIndex = (byte)paletteList.Count;
            paletteList.Add(data);
            paletteIndexDict.Add(num, addIndex);
            return addIndex;
        }
        (byte, byte, byte) Convert(ReadOnlySpan<byte> bgr)
        {
            if (bgr.Length <= 2)
            {
                throw new InvalidOperationException("Length error.");
            }

            ref readonly var b = ref bgr[0];
            ref readonly var g = ref bgr[1];
            ref readonly var r = ref bgr[2];
            var total = b * 0.0722 + g * 0.7152 + r * 0.2126;
            var calc = (byte)(total);
            switch (colorBit)
            {
                case BitmapColorBit.Bit1:
                    return (calc < byte.MaxValue / 2.0) ? ((byte)0, (byte)0, (byte)0) : ((byte)255, (byte)255, (byte)255);
                case BitmapColorBit.Bit4:
                    {
                        calc = (byte)(calc & 0xF0);
                        return (calc, calc, calc);
                    }
                case BitmapColorBit.Bit8:
                    {
                        return (calc, calc, calc);
                    }
                default:
                    throw new InvalidOperationException($"Invalid color bit. : {colorBit}");
            }
        }


        var srcImageSpan = bufferImage.AsSpan();
        var srcImageWidth = infoHeader.Width * 3;
        var writeLineByteSize = GetImageWidthSize(infoHeader.Width, colorBit);
        var writeTotalByteSize = infoHeader.Height * writeLineByteSize;
        var writeImage = ArrayPool<byte>.Shared.Rent(writeTotalByteSize);
        try
        {
            var writeImageSpan = writeImage.AsSpan(0, writeTotalByteSize);
            for (var heightIndex = 0; heightIndex < infoHeader.Height; ++heightIndex)
            {
                var srcImageIndex = infoHeader.Height - heightIndex - 1;
                var lineBitIndex = heightIndex * writeLineByteSize * 8;
                for (var widthIndex = 0; widthIndex < srcImageWidth; widthIndex += 3)
                {
                    var lineSpan = srcImageSpan.Slice(widthIndex + srcImageWidth * srcImageIndex, 3);
                    (byte g, byte b, byte r) = Convert(lineSpan);
                    var palettIndex = GetOrAddPalett(g, b, r);

                    var writeByteIndex = lineBitIndex / 8;
                    var writeBitIndex = lineBitIndex % 8;

                    switch (colorBit)
                    {
                        case BitmapColorBit.Bit1:
                            writeImageSpan[writeByteIndex] |= (byte)(palettIndex << (7 - writeBitIndex));
                            lineBitIndex += 1;
                            break;
                        case BitmapColorBit.Bit4:
                            writeImageSpan[writeByteIndex] |= (byte)(palettIndex << (3 - writeBitIndex));
                            lineBitIndex += 4;
                            break;
                        case BitmapColorBit.Bit8:
                            writeImageSpan[writeByteIndex] = (byte)(palettIndex);
                            lineBitIndex += 8;
                            break;
                        case BitmapColorBit.Bit24:
                            lineBitIndex += 24;
                            break;
                        case BitmapColorBit.Bit32:
                            lineBitIndex += 32;
                            break;
                    }
                }
            }
            // Data
            var fileHeaderSpan = ToSpan(ref fileHeader);
            var infoHeaderSpan = ToSpan(ref infoHeader);
            var paletteDataSpan = CollectionsMarshal.AsSpan(paletteList);
            var paletteByteSpan = MemoryMarshal.Cast<BitmapPaletteData, byte>(paletteDataSpan);

            fileHeader.OffBits = (uint)fileHeaderSpan.Length + (uint)infoHeaderSpan.Length + (uint)paletteByteSpan.Length;
            fileHeader.Size = fileHeader.OffBits + (uint)writeImageSpan.Length;
            infoHeader.SizeImage = (uint)writeImageSpan.Length;
            infoHeader.ClrUsed = (uint)paletteList.Count;
            infoHeader.BitCount = (ushort)colorBit;
            infoHeader.Compression = (uint)compression;

            // Save
            DeleteFile(path);
            using var fileStream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite, 4096, true);
            fileStream.Write(fileHeaderSpan);
            fileStream.Write(infoHeaderSpan);
            fileStream.Write(paletteByteSpan);
            fileStream.Write(writeImageSpan);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(writeImage);
        }

    }

    public void SaveBitFieldsImage(string path, BitmapColorBit colorBit, int redMask, int greenMask, int blueMask)
    {
        switch (colorBit)
        {
            case BitmapColorBit.Bit16:
            case BitmapColorBit.Bit32:
                break;
            default:
                throw new ArgumentException($"Compression with this bit depth is not possible.");
        }

        // Data
        var fileHeaderSpan = ToSpan(ref fileHeader);
        var infoHeaderSpan = ToSpan(ref infoHeader);

        var srcWidthSize = infoHeader.Width * 3;
        var dstImageTotalSize = (srcWidthSize) * (uint)infoHeader.Height;

        fileHeader.Size = (uint)fileHeaderSpan.Length + (uint)infoHeaderSpan.Length + (uint)dstImageTotalSize;
        fileHeader.OffBits = (uint)fileHeaderSpan.Length + (uint)infoHeaderSpan.Length + 12;
        infoHeader.SizeImage = (uint)dstImageTotalSize;
        infoHeader.BitCount = (ushort)colorBit;
        infoHeader.Compression = (uint)BitmapCompression.BitFields;

        // Save
        DeleteFile(path);
        using var fileStream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite, 4096, true);
        fileStream.Write(fileHeaderSpan);
        fileStream.Write(infoHeaderSpan);
        fileStream.Write(BitConverter.GetBytes(redMask).AsSpan());
        fileStream.Write(BitConverter.GetBytes(greenMask).AsSpan());
        fileStream.Write(BitConverter.GetBytes(blueMask).AsSpan());
        // ImageData
        (long div, long max,int shift) CalcMask(int mask)
        {
            var castMask = (long)mask;
            var calcDiv = castMask & -castMask;
            var calcMax = castMask / calcDiv;
            var calcShift = BitOperations.Log2((ulong)calcDiv);
            return (calcDiv, calcMax, calcShift);

        }
        var (redDiv, redMax,redShift) = CalcMask(redMask);
        var (greenDiv, greenMax, greenShift) = CalcMask(greenMask);
        var (blueDiv, blueMax, blueShift) = CalcMask(blueMask);

        long ScalePixelData(byte data, long max)
        {
            if(max == 0)
            {
                return 0;
            }
            var one = (double)data / (double)byte.MaxValue;
            return (long)(one * max);
        }

        for (var heightIndex = infoHeader.Height - 1; 0 <= heightIndex; --heightIndex)
        {
            for (var widthIndex = 0; widthIndex < infoHeader.Width; ++widthIndex)
            {
                var (redPixel, greenPixel, bluePixel) = GetPixel(widthIndex, heightIndex);
                switch (colorBit)
                {
                    case BitmapColorBit.Bit16:
                        {
                            var data = (short)0;
                            data |= (short)(ScalePixelData(redPixel, redMax) << redShift);
                            data |= (short)(ScalePixelData(greenPixel, greenMax) << greenShift);
                            data |= (short)(ScalePixelData(bluePixel, blueMax) << blueShift);
                            fileStream.Write(BitConverter.GetBytes(data).AsSpan());
                            break;
                        }
                    case BitmapColorBit.Bit32:
                        {
                            var data = (int)0;
                            data |= (int)(ScalePixelData(redPixel, redMax) << redShift);
                            data |= (int)(ScalePixelData(greenPixel, greenMax) << greenShift);
                            data |= (int)(ScalePixelData(bluePixel, blueMax) << blueShift);
                            fileStream.Write(BitConverter.GetBytes(data).AsSpan());
                            break;
                        }
                    default:
                        throw new ArgumentException($"Compression with this bit depth is not possible.");
                }
            }
        }
    }

    public void SaveRgb888Image(string path)
    {
        SaveBitFieldsImage(path, BitmapColorBit.Bit32, 0x00FF0000, 0x0000FF00, 0x000000FF);
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

#if NETSTANDARD2_0 || NETSTANDARD2_1
internal static class BitOperations
{
    public static int Log2(ulong value) 
    {
        return (int)Math.Log((double)value,2.0);
    }
}
#endif

