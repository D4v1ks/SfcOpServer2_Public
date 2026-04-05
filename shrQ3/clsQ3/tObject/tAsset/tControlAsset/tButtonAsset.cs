#pragma warning disable IDE1006

using System.Diagnostics.Contracts;
using System.IO;

namespace shrQ3
{
    public abstract class tButtonAsset : tControlAsset
    {
        public tTextInfo TextInfo;
        public tString TargetId;
        public int TargetNum;

        public override int Length => base.Length + TextInfo.Length + TargetId.Length + 4;

        public tButtonAsset(eType type, int id, string name) : base(type, id, name)
        {
            TextInfo = new();
            TargetId = new();
        }

        public tButtonAsset(BinaryReader r) : base(r)
        { }

        public override void ReadFrom(BinaryReader r)
        {
            base.ReadFrom(r);

            TextInfo = new(r);
            TargetId = new(r);
            TargetNum = r.ReadInt32();

            Contract.Assert(TargetNum == 0);
        }

        public override void WriteTo(BinaryWriter w)
        {
            base.WriteTo(w);

            TextInfo.WriteTo(w);
            TargetId.WriteTo(w);
            w.Write(TargetNum);
        }
    }
}
