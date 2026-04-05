#pragma warning disable IDE1006

using System.IO;

namespace shrQ3
{
    public sealed class tSliderAsset : tControlAsset
    {
        public int ThumbId;
        public tString Text;

        public override int Length => base.Length + Text.Length + 4;

#if DEBUG
        public tAsset Thumb
        {
            get
            {
                if (Parent != null && Parent.Assets.TryGetValue(ThumbId, out tAsset asset))
                    return asset;

                return null;
            }
        }
#endif

        public tSliderAsset(int id, string name) : base(eType.Slider, id, name)
        { }

        public tSliderAsset(BinaryReader r) : base(r)
        { }

        public override void ReadFrom(BinaryReader r)
        {
            base.ReadFrom(r);

            ThumbId = r.ReadInt32();
            Text = new(r);
        }

        public override void WriteTo(BinaryWriter w)
        {
            base.WriteTo(w);

            w.Write(ThumbId);
            Text.WriteTo(w);
        }
    }
}
