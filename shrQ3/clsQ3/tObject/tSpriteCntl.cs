#pragma warning disable IDE1006

using System.IO;

namespace shrQ3
{
    public sealed class tSpriteCntl : tObject
    {
        public tImages Images;
        public tImages DisableImages;
        public tImages HiliteImages;

        public override int Length => Images.Length + DisableImages.Length + HiliteImages.Length;

        public tSpriteCntl()
        {
            Images = new();
            DisableImages = new();
            HiliteImages = new();
        }

        public tSpriteCntl(BinaryReader r)
        {
            ReadFrom(r);
        }

        public override void ReadFrom(BinaryReader r)
        {
            Images = new(r);
            DisableImages = new(r);
            HiliteImages = new(r);
        }

        public override void WriteTo(BinaryWriter w)
        {
            Images.WriteTo(w);
            DisableImages.WriteTo(w);
            HiliteImages.WriteTo(w);
        }
    }
}
