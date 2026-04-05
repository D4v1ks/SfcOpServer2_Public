#pragma warning disable IDE0130, IDE0290

using System.IO;

namespace SfcOpServer
{
    internal struct VictoryCache
    {
        public int Index;
        public int Points;

        public VictoryCache(BinaryReader r)
        {
            Index = r.ReadInt32();
            Points = r.ReadInt32();
        }
    }
}
