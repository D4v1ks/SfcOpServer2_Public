#pragma warning disable IDE1006

using System.Diagnostics.Contracts;
using System.IO;

namespace shrQ3
{
    public class tVisualGroupAsset : tAsset
    {
        public int Flags;
        public tGroupCell[] Cells;
        public tCodeAsset Code;

        public override int Length => base.Length + (Cells.Length << 4) + Code.Length + 8;

        public tVisualGroupAsset(int id, string name, eType type = eType.Scene) : base(type, id, name)
        {
            Cells = [];
            Code = new();
        }

        public tVisualGroupAsset(BinaryReader r) : base(r)
        { }

        public override void ReadFrom(BinaryReader r)
        {
            base.ReadFrom(r);

            Flags = r.ReadInt32();

            Contract.Assert(Flags == 0 || Flags == 16);

            int c = r.ReadInt32();

            if (c > 0)
            {

                Cells = new tGroupCell[c];

                for (int i = 0; i < c; i++)
                    Cells[i] = new(r);
            }
            else
            {
                Contract.Assert(c == 0);

                Cells = [];
            }

            Code = new(r);
        }

        public override void WriteTo(BinaryWriter w)
        {
            base.WriteTo(w);

            w.Write(Flags);

            int c = Cells.Length;

            w.Write(c);

            for (int i = 0; i < c; i++)
                Cells[i].WriteTo(w);

            Code.WriteTo(w);
        }
    }
}
