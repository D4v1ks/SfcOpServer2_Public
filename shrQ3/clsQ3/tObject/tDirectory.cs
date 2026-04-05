#pragma warning disable IDE1006

using System.IO;

namespace shrQ3
{
    public sealed class tDirectory : tObject
    {
        public int DirectorySize;
        public int AssetsCount;

        public override int Length => 8;

        public tDirectory()
        { }

        public tDirectory(BinaryReader r)
        {
            ReadFrom(r);
        }

        public override void ReadFrom(BinaryReader r)
        {
            DirectorySize = r.ReadInt32();
            AssetsCount = r.ReadInt32();
        }

        public override void WriteTo(BinaryWriter w)
        {
            w.Write(DirectorySize);
            w.Write(AssetsCount);
        }
    }
}