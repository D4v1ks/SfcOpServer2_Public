#pragma warning disable IDE1006

using System.IO;

namespace shrQ3
{
    public abstract class tControlAsset : tAsset
    {
        public tCodeAsset Code;
        public int Flags;

        public override int Length => base.Length + Code.Length + 4;

        public tControlAsset(eType type, int id, string name) : base(type, id, name)
        {
            Code = new();
        }

        public tControlAsset(BinaryReader r) : base(r)
        { }

        public override void ReadFrom(BinaryReader r)
        {
            base.ReadFrom(r);

            Code = new(r);
            Flags = r.ReadInt32();
        }

        public override void WriteTo(BinaryWriter w)
        {
            base.WriteTo(w);

            Code.WriteTo(w);
            w.Write(Flags);
        }
    }
}
