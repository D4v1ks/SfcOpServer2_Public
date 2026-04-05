#pragma warning disable IDE1006

using System.Diagnostics.Contracts;
using System.IO;

namespace shrQ3
{
    public sealed class tImages : tObject
    {
        public int[] Id;

        public override int Length => (Id.Length << 2) + 4;

#if DEBUG
        public tAsset[] Asset
        {
            get
            {
                if (Parent != null && Id != null)
                {
                    int c = Id.Length;

                    if (c > 0)
                    {
                        tAsset[] a = new tAsset[c];

                        for (int i = 0; i < c; i++)
                            Parent.Assets.TryGetValue(Id[i], out a[i]);

                        return a;
                    }
                }

                return null;
            }
        }
#endif

        public tImages()
        {
            Id = [];
        }

        public tImages(BinaryReader r)
        {
            ReadFrom(r);
        }

        public override void ReadFrom(BinaryReader r)
        {
            int c = r.ReadInt32();

            if (c > 0)
            {
                Id = new int[c];

                for (int i = 0; i < c; i++)
                    Id[i] = r.ReadInt32();
            }
            else
            {
                Contract.Assert(c == 0);

                Id = [];
            }
        }

        public override void WriteTo(BinaryWriter w)
        {
            int c = Id.Length;

            w.Write(c);

            for (int i = 0; i < c; i++)
                w.Write(Id[i]);
        }
    }
}
