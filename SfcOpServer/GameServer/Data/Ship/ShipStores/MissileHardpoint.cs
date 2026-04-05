#pragma warning disable IDE0130

using System.IO;

namespace SfcOpServer
{
    public struct MissileHardpoint
    {
        public short MissilesReady;
        public short MissilesStored;
        public short TubesCount;
        public short TubesCapacity;

        public void ReadFrom(BinaryReader r)
        {
            MissilesReady = r.ReadInt16();
            MissilesStored = r.ReadInt16();
            TubesCount = r.ReadInt16();
            TubesCapacity = r.ReadInt16();
        }

        public readonly void WriteTo(BinaryWriter w)
        {
            w.Write(MissilesReady);
            w.Write(MissilesStored);
            w.Write(TubesCount);
            w.Write(TubesCapacity);
        }
    }
}
