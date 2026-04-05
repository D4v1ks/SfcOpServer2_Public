#pragma warning disable IDE1006

using System.IO;

namespace shrQ3
{
    public sealed class tStaticTextAsset : tBoundAsset
    {
        public tTextInfo TextInfo;

        public override int Length => base.Length + TextInfo.Length;

        public tStaticTextAsset(int id, string name) : base(eType.TextRect, id, name)
        { }

        public tStaticTextAsset(BinaryReader r) : base(r)
        { }

        public override void ReadFrom(BinaryReader r)
        {
            base.ReadFrom(r);

            TextInfo = new(r);
        }

        public override void WriteTo(BinaryWriter w)
        {
            base.WriteTo(w);

            TextInfo.WriteTo(w);
        }
    }
}
