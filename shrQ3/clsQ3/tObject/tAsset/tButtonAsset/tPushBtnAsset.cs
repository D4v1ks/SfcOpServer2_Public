#pragma warning disable IDE1006

using System.IO;

namespace shrQ3
{
    public sealed class tPushBtnAsset : tButtonAsset
    {
        public tSpriteCntl SpriteControl;

        public override int Length => base.Length + SpriteControl.Length;

        public tPushBtnAsset(int id, string name) : base(eType.Button, id, name)
        {
            SpriteControl = new();
        }

        public tPushBtnAsset(BinaryReader r) : base(r)
        { }

        public override void ReadFrom(BinaryReader r)
        {
            base.ReadFrom(r);

            SpriteControl = new(r);
        }

        public override void WriteTo(BinaryWriter w)
        {
            base.WriteTo(w);

            SpriteControl.WriteTo(w);
        }
    }
}
