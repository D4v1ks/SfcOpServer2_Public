#pragma warning disable IDE0130

using shrServices;
using System.IO;

namespace SfcOpServer
{
    internal struct CharacterCache
    {
        public string IPAddress;
        public string WONLogon;
        public int Id;
        public string CharacterName;
        public Races CharacterRace;
        public Races CharacterPoliticalControl;
        public Ranks CharacterRank;
        public int CharacterRating;
        public int CharacterCurrentPrestige;
        public int CharacterLifetimePrestige;
        public int Unknown;
        public int CharacterLocationX;
        public int CharacterLocationY;
        public int HomeWorldLocationX;
        public int HomeWorldLocationY;
        public int MoveDestinationX;
        public int MoveDestinationY;
        public int ShipCount;
        public ShipCache[] Ships;

        public CharacterCache(BinaryReader r)
        {
            Utils.Read(r, true, out IPAddress);
            Utils.Read(r, true, out WONLogon);

            Id = r.ReadInt32();

            Utils.Read(r, true, out CharacterName);

            CharacterRace = (Races)r.ReadInt32();
            CharacterPoliticalControl = (Races)r.ReadInt32();
            CharacterRank = (Ranks)r.ReadInt32();
            CharacterRating = r.ReadInt32();
            CharacterCurrentPrestige = r.ReadInt32();
            CharacterLifetimePrestige = r.ReadInt32();
            Unknown = r.ReadInt32();
            CharacterLocationX = r.ReadInt32();
            CharacterLocationY = r.ReadInt32();
            HomeWorldLocationX = r.ReadInt32();
            HomeWorldLocationY = r.ReadInt32();
            MoveDestinationX = r.ReadInt32();
            MoveDestinationY = r.ReadInt32();
            ShipCount = r.ReadInt32();

            if (ShipCount == 0)
                Ships = [];
            else
            {
                Ships = new ShipCache[ShipCount];

                for (int i = 0; i < ShipCount; i++)
                    Ships[i] = new ShipCache(r);
            }
        }
    }
}
