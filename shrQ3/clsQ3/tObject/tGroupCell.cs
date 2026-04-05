#pragma warning disable IDE1006

using System.IO;

namespace shrQ3
{
    public sealed class tGroupCell : tObject
    {
        public int Id;
        public int X;
        public int Y;
        public int Z;

        public override int Length => 16;

#if DEBUG
        public tAsset Asset
        {
            get
            {
                if (Parent != null && Parent.Assets.TryGetValue(Id, out tAsset asset))
                    return asset;

                return null;
            }
        }
#endif

        public tGroupCell(int id)
        {
            Id = id;
        }

        public tGroupCell(BinaryReader r)
        {
            ReadFrom(r);
        }

        public override void ReadFrom(BinaryReader r)
        {
            Id = r.ReadInt32();
            X = r.ReadInt32();
            Y = r.ReadInt32();
            Z = r.ReadInt32();
        }

        public override void WriteTo(BinaryWriter w)
        {
            w.Write(Id);
            w.Write(X);
            w.Write(Y);
            w.Write(Z);
        }
    }
}
