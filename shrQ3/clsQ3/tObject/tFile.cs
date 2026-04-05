#pragma warning disable IDE1006

using System.Diagnostics.Contracts;
using System.IO;

namespace shrQ3
{
    public sealed class tFile : tObject
    {
        public uint Description;
        public short Version;
        public ushort Endianness;
        public short BitDepth;
        public int DirectoryOffset;

        public override int Length => 14;

        public tFile()
        {
            Description = 0x214d444du; // MDM!
            Version = 153;
            Endianness = 256;
            BitDepth = 16;
        }

        public tFile(BinaryReader r)
        {
            ReadFrom(r);
        }

        public override void ReadFrom(BinaryReader r)
        {
            Description = r.ReadUInt32();
            Version = r.ReadInt16();
            Endianness = r.ReadUInt16();
            BitDepth = r.ReadInt16();
            DirectoryOffset = r.ReadInt32();

            Contract.Assert(Description == 0x214d444dU && Version == 153 && Endianness == 256 && BitDepth == 16 && DirectoryOffset >= Length);
        }

        public override void WriteTo(BinaryWriter w)
        {
            w.Write(Description);
            w.Write(Version);
            w.Write(Endianness);
            w.Write(BitDepth);
            w.Write(DirectoryOffset);
        }
    }
}
