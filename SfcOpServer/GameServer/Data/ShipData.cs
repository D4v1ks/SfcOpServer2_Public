#pragma warning disable IDE0130

namespace SfcOpServer
{
    public sealed class ShipData
    {
        public Races Race;
        public HullTypes HullType;
        public string ClassName;
        public ClassTypes ClassType;
        public int BPV;
        public SpecialRoles SpecialRole;
        public int YearFirstAvailable;
        public int YearLastAvailable;
        public int SizeClass;
        public string TurnMode;
        public float MoveCost;
        public int HetAndNimble;
        public int HetBreakdown;
        public int StealthOrECM;
        public float RegularCrew;
        public int BoardingPartiesBase;
        public int BoardingPartiesMax;
        public int DeckCrews;
        public float TotalCrew;
        public int MinCrew;
        public int Shield1;
        public int Shield2And6;
        public int Shield3And5;
        public int Shield4;
        public int ShieldTotal;
        public int Cloak;
        public ShipWeaponData[] Weapons;
        public int Probes;
        public int T_BombsBase;
        public int T_BombsMax;
        public int NuclearMineBase;
        public int NuclearMineMax;
        public int DroneControl;
        public int ADD_6;
        public int ADD_12;
        public int ShuttlesSize;
        public int LaunchRate;
        public int GeneralBase;
        public int GeneralMax;
        public int FighterBay1;
        public string FighterType1;
        public int FighterBay2;
        public string FighterType2;
        public int FighterBay3;
        public string FighterType3;
        public int FighterBay4;
        public string FighterType4;
        public int Armor;
        public int ForwardHull;
        public int CenterHull;
        public int AftHull;
        public int Cargo;
        public int Barracks;
        public int Repair;
        public int R_L_Warp;
        public int C_Warp;
        public int Impulse;
        public int Apr;
        public int Battery;
        public int Bridge;
        public int Security;
        public int Lab;
        public int Transporters;
        public int Tractors;
        public int MechTractors;
        public int SpecialSensors;
        public int Sensors;
        public int Scanners;
        public int ExplosionStrength;
        public int Acceleration;
        public int DamageControl;
        public int ExtraDamage;
        public int ShipCost;
        public string RefitBaseClass;
        public string Geometry;
        public string UI;
        public string FullName;
        public string Refits;
        public int Balance;

        public ShipData()
        {
            Weapons = new ShipWeaponData[25];
        }
    }
}
