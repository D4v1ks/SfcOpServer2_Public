#pragma warning disable IDE0130, IDE0290

using System.IO;

namespace SfcOpServer
{
    internal struct ControlCache
    {
        public int Index;
        public Races Race;

        public ControlCache(BinaryReader r)
        {
            Index = r.ReadInt32();
            Race = (Races)r.ReadInt32();
        }
    }
}
