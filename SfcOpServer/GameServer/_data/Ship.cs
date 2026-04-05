#pragma warning disable IDE0130

using shrServices;

using System;
using System.Diagnostics.Contracts;
using System.IO;

namespace SfcOpServer
{
    public sealed class Ship : IObject
    {
        public const int SystemsSize = 100;
        public const int MinStoresSize = 387;
        public const int MinOfficersSize = 112;

        // data

        public int Id { get; set; }

        public int LockID;
        public int OwnerID;
        public byte IsInAuction;
        public Races Race;
        public ClassTypes ClassType;
        public int BPV;
        public int EPV;
        public string ShipClassName;
        public string Name;
        public int TurnCreated;
        public ShipSystems Systems;
        public ShipStores Stores;
        public ShipOfficers Officers;

        // constructors

        public Ship()
        { }

        public Ship(BinaryReader r)
        {
            // data

            Id = r.ReadInt32();
            LockID = r.ReadInt32();
            OwnerID = r.ReadInt32();
            IsInAuction = r.ReadByte();
            Race = (Races)r.ReadInt32();
            ClassType = (ClassTypes)r.ReadInt32();
            BPV = r.ReadInt32();
            EPV = r.ReadInt32();

            Utils.Read(r, false, out ShipClassName);
            Utils.Read(r, false, out Name);

            TurnCreated = r.ReadInt32();

            Systems = new ShipSystems(r);
            Stores = new ShipStores(r);
            Officers = new ShipOfficers(r);

            r.ReadInt32();
        }

        public void WriteTo(BinaryWriter w)
        {
            w.Write(Id);
            w.Write(LockID);
            w.Write(OwnerID);
            w.Write(IsInAuction);
            w.Write((int)Race);
            w.Write((int)ClassType);
            w.Write(BPV);
            w.Write(EPV);

            Utils.Write(w, false, ShipClassName);
            Utils.Write(w, false, Name);

            w.Write(TurnCreated);

            Systems.WriteTo(w);
            Stores.WriteTo(w);
            Officers.WriteTo(w);

            w.Write(0x00);
        }

        // public functions

#if DEBUG
        public static int GetShipSize(byte[] buffer, int index)
        {
            int p = index;

            // header

            int c = GetHeaderSize(buffer, p);

            p += c;

            // systems

            p += SystemsSize;

            // stores

            c = GetStoresSize(buffer, p);
            p += c;

            // officers

            c = GetOfficersSize(buffer, p);
            p += c;

            // flags

            p += 4;

            return p - index;
        }
#endif

        public static int GetHeaderSize(byte[] buffer, int index)
        {
            Contract.Assert(buffer.Length >= index + SystemsSize);

            int p = index;

            // first part

            p += 29;

            // ship class name

            int c = BitConverter.ToInt32(buffer, p);

            p += 4;
            p += c;

            // name

            c = BitConverter.ToInt32(buffer, p);

            p += 4;
            p += c;

            // last part

            p += 4;

            return p - index;
        }

        public static int GetStoresSize(byte[] buffer, int index)
        {
            Contract.Assert(buffer.Length >= index + MinStoresSize);

            int p = index;

            // 2nd section

            p += ShipStores.OffsetSection2;

            int c = BitConverter.ToInt32(buffer, p);

            p += 4;
            p += c * ShipStores.SizeSection2;

            // 3rd section

            p += ShipStores.SizeSection3;

            // 4th section

            p += ShipStores.SizeSection4;

            // 5th section

            for (int i = 0; i < 4; i++)
            {
                p += 4;

                c = BitConverter.ToInt32(buffer, p);

                p += 4;
                p += c;

                c = BitConverter.ToInt32(buffer, p);

                p += 4;
                p += c;
            }

            return p - index;
        }

        public static int GetOfficersSize(byte[] buffer, int index)
        {
            Contract.Assert(buffer.Length >= index + MinOfficersSize);

            int p = index;

            for (int i = 0; i < 7; i++)
            {
                // officer name

                int c = BitConverter.ToInt32(buffer, p);

                p += 4;
                p += c;

                // last part

                p += 12;
            }

            return p - index;
        }
    }
}
