#pragma warning disable IDE0130

namespace SfcOpServer
{
    public sealed class FighterData
    {
        public Races Race;
        public string HullType;
        public int Speed;
        public FighterWeaponData[] Weapons;
        public int Damage;
        public int ADD_6;
        public int GroundAttackBonus;
        public int ECM;
        public int ECCM;
        public int BPV;
        public int CarrierSizeClass;
        public int FirstYearAvailable;
        public int LastYearAvailable;
        public int Size;
        public string UI;
        public string Geometry;

        public FighterData()
        {
            Weapons = new FighterWeaponData[5];
        }
    }
}
