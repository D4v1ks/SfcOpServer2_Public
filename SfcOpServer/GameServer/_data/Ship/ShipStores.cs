#pragma warning disable IDE0130

using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;

namespace SfcOpServer
{
    public sealed class ShipStores
    {
        public const int OffsetSection2 = 226;

        public const int SizeSection2 = 8;
        public const int SizeSection3 = 100;
        public const int SizeSection4 = 9;

        public bool ContainsFighters => FighterBays[0].FighterType.Length + FighterBays[1].FighterType.Length + FighterBays[2].FighterType.Length + FighterBays[3].FighterType.Length != 0;

        // 1st section

        public MissileTypes MissilesType; // byte
        public MissileDriveSystems MissilesDriveSystem; // byte
        public short MissilesReloads;

        public short TotalTubesCount;
        public short TotalMissilesReadyAndStored;
        public short TotalMissilesReady;
        public short TotalMissilesStored;

        public MissileHardpoint[] MissileHardpoints;

        public StoreItem General; // shuttles

        public StoreItem Unknown3;
        public StoreItem Unknown4;
        public StoreItem Unknown5;

        // 2nd section

        public SortedDictionary<TransportItems, int> TransportItems;

        // 3rd section

        public WeaponHardpoint[] WeaponHardpoints;

        // 4th section

        public StoreItem BoardingParties;
        public StoreItem TBombs;
        public StoreItem DamageControl; // spare parts

        // 5th section

        public FighterBay[] FighterBays;

        public ShipStores(byte[] buffer, int index, int count)
        {
            using MemoryStream m = new(buffer, index, count);
            using BinaryReader r = new(m);

            Initialize();
            ReadFrom(r);

            Contract.Assert(r.BaseStream.Position == count);
        }

        public ShipStores(BinaryReader r)
        {
            Initialize();
            ReadFrom(r);
        }

        public void ReadFrom(BinaryReader r)
        {
            // 1st section

            r.ReadByte(); // 1
            r.ReadByte(); // 1

            MissilesType = (MissileTypes)r.ReadByte();
            MissilesDriveSystem = (MissileDriveSystems)r.ReadByte();
            MissilesReloads = r.ReadInt16();

            TotalTubesCount = r.ReadInt16();
            TotalMissilesReadyAndStored = r.ReadInt16();
            TotalMissilesReady = r.ReadInt16();
            TotalMissilesStored = r.ReadInt16();

            for (int i = 0; i < 25; i++)
                MissileHardpoints[i].ReadFrom(r);

            General.ReadFrom(r);

            Unknown3.ReadFrom(r);
            Unknown4.ReadFrom(r);
            Unknown5.ReadFrom(r);

            // 2nd section

            int c = r.ReadInt32();

            TransportItems.Clear();

            for (int i = 0; i < c; i++)
            {
                TransportItems item = (TransportItems)r.ReadInt32();
                int count = r.ReadInt32();

                TransportItems.Add(item, count);
            }

            // 3rd section

            for (int i = 0; i < 25; i++)
                WeaponHardpoints[i].ReadFrom(r);

            // 4th section

            BoardingParties.ReadFrom(r);
            TBombs.ReadFrom(r);
            DamageControl.ReadFrom(r);

            // 5th section

            for (int i = 0; i < 4; i++)
                FighterBays[i].ReadFrom(r);
        }

        public void WriteTo(BinaryWriter w)
        {
            // 1st section

            w.Write((byte)0x01);
            w.Write((byte)0x01);

            w.Write((byte)MissilesType);
            w.Write((byte)MissilesDriveSystem);
            w.Write(MissilesReloads);

            w.Write(TotalTubesCount);
            w.Write(TotalMissilesReadyAndStored);
            w.Write(TotalMissilesReady);
            w.Write(TotalMissilesStored);

            for (int i = 0; i < 25; i++)
                MissileHardpoints[i].WriteTo(w);

            General.WriteTo(w);

            Unknown3.WriteTo(w);
            Unknown4.WriteTo(w);
            Unknown5.WriteTo(w);

            // 2nd section

            int c = TransportItems.Count;

            w.Write(c);

            foreach (KeyValuePair<TransportItems, int> p in TransportItems)
            {
                w.Write((int)p.Key);
                w.Write(p.Value);
            }

            // 3rd section

            for (int i = 0; i < 25; i++)
                WeaponHardpoints[i].WriteTo(w);

            // 4th section

            BoardingParties.WriteTo(w);
            TBombs.WriteTo(w);
            DamageControl.WriteTo(w);

            // 5th section

            for (int i = 0; i < 4; i++)
                FighterBays[i].WriteTo(w);
        }

        private void Initialize()
        {
            MissileHardpoints = new MissileHardpoint[25];

            TransportItems = [];

            WeaponHardpoints = new WeaponHardpoint[25];

            FighterBays = new FighterBay[4];
        }
    }
}
