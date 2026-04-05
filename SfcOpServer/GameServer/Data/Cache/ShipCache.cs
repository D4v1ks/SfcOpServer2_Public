#pragma warning disable IDE0130

using shrServices;
using System.IO;

namespace SfcOpServer
{
    internal struct ShipCache
    {
        public int Id;
        public int BPV;
        public ClassTypes ClassType;
        public string ShipClassName;
        public float EpvRatio;
        public string Name;
        public int Flags;

        public ShipCache(BinaryReader r)
        {
            Id = r.ReadInt32();
            BPV = r.ReadInt32();
            ClassType = (ClassTypes)r.ReadInt32();

            Utils.Read(r, true, out ShipClassName);

            EpvRatio = r.ReadSingle();

            Utils.Read(r, true, out Name);

            Flags = r.ReadInt32();
        }
    }
}
