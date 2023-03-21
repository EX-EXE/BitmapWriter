using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace BitmapWriter;

[StructLayout(LayoutKind.Explicit, Pack = 1)]
internal struct BitmapInfoHeader
{
    public BitmapInfoHeader()
    {
    }

    [FieldOffset(0)]
    public uint Size = 40;
    [FieldOffset(4)]
    public int Width = 0;
    [FieldOffset(8)]
    public int Height = 0;
    [FieldOffset(12)]
    public short Planes = 1;
    [FieldOffset(14)]
    public ushort BitCount = (ushort)BitmapColorBit.Bit1;
    [FieldOffset(16)]
    public uint Compression = (uint)BitmapCompression.Rgb;
    [FieldOffset(20)]
    public uint SizeImage = 0;
    [FieldOffset(24)]
    public int XPixPerMeter = 0;
    [FieldOffset(28)]
    public int YPixPerMeter = 0;
    [FieldOffset(32)]
    public uint ClrUsed = 0;
    [FieldOffset(36)]
    public uint CirImportant = 0;

}
