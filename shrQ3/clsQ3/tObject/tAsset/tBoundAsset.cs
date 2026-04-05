#pragma warning disable IDE1006

using System.IO;

namespace shrQ3
{
    public abstract class tBoundAsset : tAsset
    {
        public tPoint Extent;

        public override int Length => base.Length + Extent.Length;

        public tBoundAsset(eType type, int id, string name) : base(type, id, name)
        {
            Extent = new();
        }

        public tBoundAsset(BinaryReader r) : base(r)
        { }

        public override void ReadFrom(BinaryReader r)
        {
            base.ReadFrom(r);

            Extent = new(r);
        }

        public override void WriteTo(BinaryWriter w)
        {
            base.WriteTo(w);

            Extent.WriteTo(w);
        }
    }
}
