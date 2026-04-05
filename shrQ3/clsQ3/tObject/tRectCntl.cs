#pragma warning disable IDE1006

using System.Diagnostics.Contracts;
using System.IO;

namespace shrQ3
{
    public sealed class tRectCntl : tObject
    {
        public enum Enum1
        {
            Normal,
            Down,
            Disabled,
            Hilited,

            Max
        }

        public enum Enum2
        {
            Face,
            Top,
            Bottom,

            Max
        }

        public uint[][] Colors;

        public override int Length => Colors.Length * 12;

        public tRectCntl()
        {
            Colors = [];
        }

        public tRectCntl(BinaryReader r)
        {
            ReadFrom(r);
        }

        public override void ReadFrom(BinaryReader r)
        {
            Colors = new uint[(int)Enum1.Max][];

            for (int i = 0; i < (int)Enum1.Max; i++)
            {
                Colors[i] = new uint[(int)Enum2.Max];

                for (int j = 0; j < (int)Enum2.Max; j++)
                    Colors[i][j] = r.ReadUInt32();
            }
        }

        public override void WriteTo(BinaryWriter w)
        {
            Contract.Assert(Colors.Length == (int)Enum1.Max);

            for (int i = 0; i < (int)Enum1.Max; i++)
            {
                Contract.Assert(Colors[i].Length == (int)Enum2.Max);

                for (int j = 0; j < (int)Enum2.Max; j++)
                    w.Write(Colors[i][j]);
            }
        }
    }
}
