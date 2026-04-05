#pragma warning disable IDE0130

using System;
using System.Diagnostics.Contracts;
using System.IO;

namespace SfcOpServer
{
    public sealed class ShipSystems
    {
        public byte[] Items;

        public ShipSystems(byte[] buffer, int index)
        {
            Contract.Assert(buffer.Length >= index + Ship.SystemsSize);

            Items = new byte[Ship.SystemsSize];

            Buffer.BlockCopy(buffer, index, Items, 0, Ship.SystemsSize);
        }

        public ShipSystems(BinaryReader r)
        {
            ReadFrom(r);
        }

        public void ReadFrom(BinaryReader r)
        {
            Items = r.ReadBytes(Ship.SystemsSize);
        }

        public void WriteTo(BinaryWriter w)
        {
            w.Write(Items);
        }
    }
}
