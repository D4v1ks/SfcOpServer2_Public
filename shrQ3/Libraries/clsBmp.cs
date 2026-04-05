#pragma warning disable CA1416, IDE1006

// it requires the NuGet "System.Drawing.Common" to work

using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;

namespace shrQ3
{
    public sealed class clsBmp
    {
        // enumerations

        public enum BitmapCompressionTypes
        {
            RGB = 0,
            RLE8 = 1,
            RLE4 = 2,
            BITFIELDS = 3,
            JPEG = 4,
            PNG = 5,
            CMYK = 11,
            CMYKRLE8 = 12,
            CMYKRLE4 = 13
        }

        // structures

        [StructLayout(LayoutKind.Sequential, Pack = 2)]
        public struct BitmapFileHeader
        {
            public ushort Type;
            public uint FileSize;
            public ushort Reserverd1;
            public ushort Reserverd2;
            public uint BitmapOffset;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 2)]
        public struct BitmapV5Header
        {
            public uint Size;
            public int Width;
            public int Height;
            public ushort Planes;
            public ushort BitCount;
            public uint Compression;
            public uint SizeImage;
            public int XPelsPerMeter;
            public int YPelsPerMeter;
            public uint ColorsUsed;
            public uint ColorsImportant;

            public uint RedMask;
            public uint GreenMask;
            public uint BlueMask;
            public uint AlphaMask;
            public uint CSType;
            public uint RedX;
            public uint RedY;
            public uint RedZ;
            public uint GreenX;
            public uint GreenY;
            public uint GreenZ;
            public uint BlueX;
            public uint BlueY;
            public uint BlueZ;
            public uint GamaRed;
            public uint GamaGreen;
            public uint GamaBlue;

            public uint Intent;
            public uint ProfileData;
            public uint ProfileSize;
            public uint Reserverd;
        }

        // functions

        public static unsafe void GenerateBitmap(int width, int height, int bitCount, out BitmapFileHeader fileHeader, out BitmapV5Header bitmapHeader)
        {
            Contract.Assert(sizeof(BitmapFileHeader) == 14 && sizeof(BitmapV5Header) == 124);

            int stride = (width * bitCount + 31 & ~31) >> 3;
            uint sizeImage = (uint)(height * stride);

            bitmapHeader = new BitmapV5Header()
            {
                Size = (uint)(sizeof(BitmapV5Header)),
                Width = width,
                Height = height,
                Planes = 1,
                BitCount = (ushort)bitCount,

                SizeImage = sizeImage,
                XPelsPerMeter = 3780, // 96 DPI
                YPelsPerMeter = 3780, // 96 DPI

                CSType = 1934772034,

                Intent = 2
            };

            if (bitCount == 8)
            {
                bitmapHeader.ColorsUsed = 256;
            }
            else if (bitCount == 16)
            {
                bitmapHeader.Compression = (uint)BitmapCompressionTypes.BITFIELDS;

                bitmapHeader.RedMask = 31744;
                bitmapHeader.GreenMask = 992;
                bitmapHeader.BlueMask = 31;
            }
            else if (bitCount == 32)
            {
                bitmapHeader.Compression = (uint)BitmapCompressionTypes.BITFIELDS;

                bitmapHeader.RedMask = 16711680;
                bitmapHeader.GreenMask = 65280;
                bitmapHeader.BlueMask = 255;
            }

            uint bitmapOffset = (uint)(sizeof(BitmapFileHeader) + sizeof(BitmapV5Header)) + (bitmapHeader.ColorsUsed << 2);

            fileHeader = new BitmapFileHeader()
            {
                Type = 19778,
                FileSize = bitmapOffset + sizeImage,

                BitmapOffset = bitmapOffset
            };
        }
    }
}
