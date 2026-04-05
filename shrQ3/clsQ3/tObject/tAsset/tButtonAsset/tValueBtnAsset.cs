#pragma warning disable IDE1006

using System.IO;

namespace shrQ3
{
    public sealed class tValueBtnAsset : tButtonAsset
    {
        public short MaxValue;
        public tSpriteCntl SpriteControl;

        public override int Length => base.Length + SpriteControl.Length + 2;

        public tValueBtnAsset(int id, string name) : base(eType.StateBtn, id, name)
        {
            SpriteControl = new();
        }

        public tValueBtnAsset(BinaryReader r) : base(r)
        { }

        public override void ReadFrom(BinaryReader r)
        {
            base.ReadFrom(r);

            MaxValue = r.ReadInt16();
            SpriteControl = new(r);
        }

        public override void WriteTo(BinaryWriter w)
        {
            base.WriteTo(w);

            w.Write(MaxValue);
            SpriteControl.WriteTo(w);
        }
    }
}
