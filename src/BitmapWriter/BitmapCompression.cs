using System;
using System.Collections.Generic;
using System.Text;

namespace BitmapWriter;

public enum BitmapCompression : uint
{
    Rgb = 0,
    Rle8 = 1,
    Rle4 = 2,
    BitFields = 3
}
