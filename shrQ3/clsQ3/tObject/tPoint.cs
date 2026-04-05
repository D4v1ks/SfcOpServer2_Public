#pragma warning disable IDE1006

using System.IO;

namespace shrQ3
{
    public sealed class tPoint : tObject
    {
        public short X;
        public short Y;

        public override int Length => 4;

        public tPoint()
        { }

        public tPoint(BinaryReader r)
        {
            ReadFrom(r);
        }

        public override void ReadFrom(BinaryReader r)
        {
            X = r.ReadInt16();
            Y = r.ReadInt16();
        }

        public override void WriteTo(BinaryWriter w)
        {
            w.Write(X);
            w.Write(Y);
        }
    }
}
