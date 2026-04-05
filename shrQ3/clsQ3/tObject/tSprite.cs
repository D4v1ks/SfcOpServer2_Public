#pragma warning disable IDE1006

using System.Diagnostics.Contracts;
using System.Drawing;
using System.IO;

namespace shrQ3
{
    public sealed class tSprite : tObject
    {
        public const int HeaderSize = 20;

        public enum eCompression
        {
            Raw,
            None,
            Runlen,
            Band,
            Jpeg
        }

        public short SizeX;
        public short SizeY;
        public short RefX;
        public short RefY;
        public ushort Reserved;
        public short BitDepth;
        public short Opacity;
        public eCompression Compression;
        public byte[] Data;

        public override int Length => Data == null ? HeaderSize : Data.Length + HeaderSize;

#if DEBUG
        public Bitmap Bitmap
        {
            get
            {
                if (clsQ3.TryConvert(this, out Bitmap bitmap))
                    return bitmap;

                return null;
            }
        }
#endif

        public tSprite()
        { }

        public tSprite(BinaryReader r)
        {
            ReadFrom(r);
        }

        public override void ReadFrom(BinaryReader r)
        {
            SizeX = r.ReadInt16();
            SizeY = r.ReadInt16();
            RefX = r.ReadInt16();
            RefY = r.ReadInt16();
            Reserved = r.ReadUInt16();
            BitDepth = r.ReadInt16();
            Opacity = r.ReadInt16();
            Compression = (eCompression)r.ReadInt16();

            int c = r.ReadInt32();

            if (c > 0)
                Data = r.ReadBytes(c);
            else
            {
                Contract.Assert(c == 0);

                Data = [];
            }

            Contract.Assert(Reserved == 0 && (BitDepth == 8 | BitDepth == 16));
        }

        public override void WriteTo(BinaryWriter w)
        {
            w.Write(SizeX);
            w.Write(SizeY);
            w.Write(RefX);
            w.Write(RefY);
            w.Write(Reserved);
            w.Write(BitDepth);
            w.Write(Opacity);
            w.Write((short)Compression);

            int c = Data.Length;

            w.Write(c);

            if (c > 0)
                w.Write(Data);
        }
    }
}
