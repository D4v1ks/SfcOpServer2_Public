#pragma warning disable IDE1006

using System.IO;

namespace shrQ3
{
    public sealed class tTextEditAsset : tControlAsset
    {
        public tString Text;
        public tTextInfo TextInfo;
        public tRectCntl RectCntl;
        public short MaxLength;

        public override int Length => base.Length + Text.Length + TextInfo.Length + RectCntl.Length + 2;

        public tTextEditAsset(int id, string name) : base(eType.TextEdit, id, name)
        { }

        public tTextEditAsset(BinaryReader r) : base(r)
        { }

        public override void ReadFrom(BinaryReader r)
        {
            base.ReadFrom(r);

            Text = new(r);
            TextInfo = new(r);
            RectCntl = new(r);
            MaxLength = r.ReadInt16();
        }

        public override void WriteTo(BinaryWriter w)
        {
            base.WriteTo(w);

            Text.WriteTo(w);
            TextInfo.WriteTo(w);
            RectCntl.WriteTo(w);
            w.Write(MaxLength);
        }
    }
}
