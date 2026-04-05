#pragma warning disable IDE1006

using System.IO;

namespace shrQ3
{
    public sealed class tBmpAsset : tAsset
    {
        public int FileSize;
        public int FileOffset;
        public short RequestCount;

        public string HashKey;

        public override int Length => base.Length + 10;

#if DEBUG
        public tSprite Sprite
        {
            get
            {
                if (Parent != null && HashKey != null && Parent.Sprites.TryGetValue(HashKey, out tSprite sprite))
                    return sprite;

                return null;
            }
        }
#endif

        public tBmpAsset(int id, string name) : base(eType.Bmp, id, name)
        { }

        public tBmpAsset(BinaryReader r) : base(r)
        { }

        public override void ReadFrom(BinaryReader r)
        {
            base.ReadFrom(r);

            FileSize = r.ReadInt32();
            FileOffset = r.ReadInt32();
            RequestCount = r.ReadInt16();
        }

        public override void WriteTo(BinaryWriter w)
        {
            base.WriteTo(w);

            w.Write(FileSize);
            w.Write(FileOffset);
            w.Write(RequestCount);
        }
    }
}
