#pragma warning disable IDE0130

using System.Diagnostics.Contracts;
using System.IO;

namespace SfcOpServer
{
    public sealed class ShipOfficers
    {
        public Officer[] Items;

        public ShipOfficers(byte[] buffer, int index, int count)
        {
            using MemoryStream m = new(buffer, index, count);
            using BinaryReader r = new(m);

            ReadFrom(r);

            Contract.Assert(r.BaseStream.Position == count);
        }

        public ShipOfficers(BinaryReader r)
        {
            ReadFrom(r);
        }

        public void ReadFrom(BinaryReader r)
        {
            Items = new Officer[(int)OfficerTypes.kMaxOfficers];

            for (int i = 0; i < (int)OfficerTypes.kMaxOfficers; i++)
                Items[i].ReadFrom(r);
        }

        public void WriteTo(BinaryWriter w)
        {
            for (int i = 0; i < (int)OfficerTypes.kMaxOfficers; i++)
                Items[i].WriteTo(w);
        }
    }
}
