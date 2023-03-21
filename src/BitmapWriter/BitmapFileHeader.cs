using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace BitmapWriter;

[StructLayout(LayoutKind.Explicit, Pack = 1)]
internal struct BitmapFileHeader
{
    public BitmapFileHeader()
    {
    }
    [FieldOffset(0)]
    public ushort Type = 0x4D42;
    [FieldOffset(2)]
    public uint Size = 0;
    [FieldOffset(6)]
    public ushort Reserved1 = 0;
    [FieldOffset(8)]
    public ushort Reserved2 = 0;
    [FieldOffset(10)]
    public uint OffBits = 0;

}
