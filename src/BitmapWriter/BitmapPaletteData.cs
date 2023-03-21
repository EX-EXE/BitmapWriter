using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace BitmapWriter;

[StructLayout(LayoutKind.Explicit, Pack = 1)]
internal struct BitmapPaletteData
{
    public BitmapPaletteData()
    {
    }
    public BitmapPaletteData(byte red, byte green, byte blue)
    {
        Red = red;
        Green = green;
        Blue = blue;
    }

    [FieldOffset(0)]
    public byte Red = 0;
    [FieldOffset(1)]
    public byte Green = 0;
    [FieldOffset(2)]
    public byte Blue = 0;
    [FieldOffset(3)]
    public byte Reserve = 0;
}
