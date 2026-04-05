#pragma warning disable IDE1006

using System.Diagnostics.Contracts;
using System.IO;

namespace shrQ3
{
    public sealed class tTextInfo : tObject
    {
        public enum eFontStyles
        {
            AlignLeft = 0x1000000,
            AlignMiddle = 0x2000000,
            AlignRight = 0x3000000
        }

        public enum eFontStates
        {
            Normal,
            Hilite,
            Disable,
        }

        public tString Text;
        public eFontStyles FontStyle;
        public ushort Index;
        public uint[] Colors;

        public override int Length => Text.Length + Colors.Length * 4 + 6;

        public tTextInfo()
        {
            Text = new();
            Colors = [];
        }

        public tTextInfo(BinaryReader r)
        {
            ReadFrom(r);
        }

        public override void ReadFrom(BinaryReader r)
        {
            Text = new(r);
            FontStyle = (eFontStyles)r.ReadUInt32();
            Index = r.ReadUInt16();

            Colors =
            [
                r.ReadUInt32(),
                r.ReadUInt32(),
                r.ReadUInt32()
            ];
        }

        public override void WriteTo(BinaryWriter w)
        {
            Text.WriteTo(w);
            w.Write((uint)FontStyle);
            w.Write(Index);

            Contract.Assert(Colors.Length == 3);

            w.Write(Colors[(int)eFontStates.Normal]);
            w.Write(Colors[(int)eFontStates.Hilite]);
            w.Write(Colors[(int)eFontStates.Disable]);
        }
    }
}
