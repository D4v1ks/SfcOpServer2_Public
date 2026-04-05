using shrGF;
using shrPcg;

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace SfcOpServer
{
    public partial class GameServer
    {
        public class Fleet
        {
            public int BPV;

            public Dictionary<int, int> Ships; // shipId, shipBPV

            public float Power;

            public float Shields;
            public float Armor;
            public float Hull;
            public float ExtraDamage;

            public int[] Weapons;

            public Fleet()
            {
                Ships = [];
                Weapons = new int[(int)WeaponTypes.Total];
            }
        }

        public struct Weapon(WeaponTypes type, int count)
        {
            public WeaponTypes Type = type;
            public int Count = count;
        }

        float[] _missilePoints;
        float[] _antiMissilePoints;

        float[] _plasmaPoints;
        float[] _heavyWeaponPoints;

        float[][] _averageDamage;
        float[] _powerStarvation;

        public void LoadCpuBattleSettings()
        {
            GFFile gf = new();

            gf.Load(_root + "CpuBattles.gf");

            _missilePoints = new float[(int)WeaponTypes.Total];
            _antiMissilePoints = new float[(int)WeaponTypes.Total];

            _plasmaPoints = new float[(int)WeaponTypes.Total];
            _heavyWeaponPoints = new float[(int)WeaponTypes.Total];

            _averageDamage = new float[(int)WeaponTypes.Total][];
            _powerStarvation = new float[(int)WeaponTypes.Total];

            for (int i = 0; i < (int)WeaponTypes.Total; i++)
            {
                string name = Enum.GetName(typeof(WeaponTypes), (WeaponTypes)i);

                _missilePoints[i] = gf.GetValue("Missile_points", name, 0f);
                _antiMissilePoints[i] = gf.GetValue("Antimissile_points", name, 0f);

                _plasmaPoints[i] = gf.GetValue("Plasma_points", name, 0f);
                _heavyWeaponPoints[i] = gf.GetValue("HeavyWeapon_points", name, 0f);

                gf.TryGetValue("AverageDamage", name, out _averageDamage[i]);

                _powerStarvation[i] = gf.GetValue("Starvation", name, 0f);
            }
        }

        public void ProcessCpuBattles()
        {
            return;

            for (int i = 0; i < _map.Length; i++)
            {
                MapHex hex = _map[i];

                if (hex.IsEmpireHome)
                {
                    Dictionary<int, object> population = hex.Population;

                    if (population.Count > 10)
                    {
                        int allyMask = (int)_alliances[(int)hex.EmpireControl];
                        int neutralMask = (int)_alliances[(int)Races.kNeutralRace];

                        Fleet alliedFleet = new();
                        Fleet enemyFleet = new();

                        foreach (KeyValuePair<int, object> p in population)
                        {
                            Character character = _characters[p.Key];
                            int raceMask = 1 << (int)character.CharacterRace;

                            if ((raceMask & allyMask) != 0)
                                CreateFleet(character, alliedFleet);
                            else if ((raceMask & neutralMask) == 0)
                                CreateFleet(character, enemyFleet);
                        }

                        //List<Weapon> alliedWeapons = new();
                        //List<Weapon> enemyWeapons = new();

                        //float am = 0;
                        //float ap = 0;

                        //float em = 0;
                        //float ep = 0;

                        //for (int j = 0; j < (int)WeaponTypes.Total; j++)
                        //{
                        //    am += alliedFleet.Weapons[j] * _missilePoints[j] - enemyFleet.Weapons[j] * _antiMissilePoints[j];
                        //    ap += alliedFleet.Weapons[j] * _plasmaPoints[j] - enemyFleet.Weapons[j] * _heavyWeaponPoints[j];

                        //    em += enemyFleet.Weapons[j] * _missilePoints[j] - alliedFleet.Weapons[j] * _antiMissilePoints[j];
                        //    ep += enemyFleet.Weapons[j] * _plasmaPoints[j] - alliedFleet.Weapons[j] * _heavyWeaponPoints[j];
                        //}

                        //for (int j = 0; j < (int)WeaponTypes.Total; j++)
                        //{
                        //    alliedWeapons.Add(new Weapon((WeaponTypes)i, alliedFleet.Weapons[j]));
                        //    enemyWeapons.Add(new Weapon((WeaponTypes)i, enemyFleet.Weapons[j]));
                        //}

                        //if (am > 0f)
                        //    TradePointsForWeapons((int)Math.Round(am), enemyWeapons);
                    }
                }
            }
        }

        private void CreateFleet(Character character, Fleet fleet)
        {
            fleet.BPV += character.ShipListBPV;

            for (int j = 0; j < character.ShipCount; j++)
            {
                Ship ship = character.GetShipAt(j);

                if (((1 << (int)ship.ClassType) & ClassTypeIconMask) != 0)
                    AddToFleet(fleet, ship);
            }
        }

        private void AddToFleet(Fleet fleet, Ship ship)
        {
            fleet.Ships.Add(ship.Id, ship.BPV);

            // power stats

            byte[] systems = ship.Systems.Items;

            fleet.Power +=
                systems[(int)SystemTypes.RightWarp] +
                systems[(int)SystemTypes.LeftWarp] +
                systems[(int)SystemTypes.CenterWarp] +
                (systems[(int)SystemTypes.Apr] >> 1) +
                (systems[(int)SystemTypes.Impulse] >> 2) +
                (systems[(int)SystemTypes.Battery] >> 3);

            // defense stats

            ShipData data = _shiplist[ship.ShipClassName];

            fleet.Shields += data.ShieldTotal;
            fleet.Armor += systems[(int)SystemTypes.Armor];
            fleet.Hull += systems[(int)SystemTypes.ForwardHull] + systems[(int)SystemTypes.AfterwardHull] + systems[(int)SystemTypes.CenterHull];
            fleet.ExtraDamage += systems[(int)SystemTypes.ExtraDamage];

            // weapon stats

            NormalizeHardPointStates(ship);
        }

        private static void TradePointsForWeapons(int points, List<Weapon> weapons)
        {
            for (int i = 0; i < points; i++)
            {
                int j = clsPcg.Shared.NextInt32(weapons.Count);

                Weapon weapon = weapons[j];

                Contract.Assert(weapon.Count > 0);

                if (weapon.Count == 1)
                    weapons.RemoveAt(j);
                else
                    weapons[j] = new(weapon.Type, weapon.Count - 1);
            }
        }
    }
}
