#pragma warning disable IDE0130

using shrServices;

using System.Diagnostics.Contracts;
using System.IO;

namespace SfcOpServer
{
    public struct FighterBay
    {
        public byte FightersCount;
        public byte FightersLoaded;
        public byte FightersMax;
        public byte Unknown1; // Size?

        public string FighterType;
        public int Unknown2; // Name?

        public void ReadFrom(BinaryReader r)
        {
            FightersCount = r.ReadByte();
            FightersLoaded = r.ReadByte();
            FightersMax = r.ReadByte();
            Unknown1 = r.ReadByte();

            Utils.Read(r, false, out FighterType);

            Unknown2 = r.ReadInt32();

            Contract.Assert(Unknown2 == 0); 
        }

        public readonly void WriteTo(BinaryWriter w)
        {
            Contract.Assert(Unknown2 == 0);

            w.Write(FightersCount);
            w.Write(FightersLoaded);
            w.Write(FightersMax);
            w.Write(Unknown1);

            Utils.Write(w, false, FighterType);

            w.Write(Unknown2);
        }
    }
}
