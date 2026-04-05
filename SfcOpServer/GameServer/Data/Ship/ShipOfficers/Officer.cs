#pragma warning disable IDE0130

using shrServices;
using System.IO;

namespace SfcOpServer
{
    public struct Officer
    {
        public string Name;
        public OfficerRanks Rank;
        public int Unknown1;
        public int Unknown2;

        public void ReadFrom(BinaryReader r)
        {
            Utils.Read(r, false, out Name);

            Rank = (OfficerRanks)r.ReadInt32();
            Unknown1 = r.ReadInt32();
            Unknown2 = r.ReadInt32();
        }

        public readonly void WriteTo(BinaryWriter w)
        {
            Utils.Write(w, false, Name);

            w.Write((int)Rank);
            w.Write(Unknown1);
            w.Write(Unknown2);
        }
    }
}
