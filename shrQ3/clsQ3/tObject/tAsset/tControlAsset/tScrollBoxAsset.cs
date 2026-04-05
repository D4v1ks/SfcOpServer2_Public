#pragma warning disable IDE1006

using System.Diagnostics.Contracts;
using System.IO;

namespace shrQ3
{
    public sealed class tScrollBoxAsset : tControlAsset
    {
        public int ChildNum;
        public uint Flags2;
        public tString ChildName;

        public override int Length => base.Length + ChildName.Length + 8;

#if DEBUG
        public tAsset Asset
        {
            get
            {
                if (Parent != null && ChildName != null && Parent.Names.TryGetValue(ChildName.Value, out int id) && Parent.Assets.TryGetValue(id, out tAsset a))
                    return a;

                return null;
            }
        }
#endif

        public tScrollBoxAsset(int id, string name) : base(eType.ScrollBox, id, name)
        {
            ChildName = new();
        }

        public tScrollBoxAsset(BinaryReader r) : base(r)
        { }

        public override void ReadFrom(BinaryReader r)
        {
            base.ReadFrom(r);

            ChildNum = r.ReadInt32();
            Flags2 = r.ReadUInt32();
            ChildName = new(r);

            Contract.Assert(ChildNum == 0);
        }

        public override void WriteTo(BinaryWriter w)
        {
            base.WriteTo(w);

            w.Write(ChildNum);
            w.Write(Flags2);
            ChildName.WriteTo(w);
        }
    }
}
