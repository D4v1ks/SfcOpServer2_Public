//#define DISPLAY_MAP

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

namespace SfcOpServer
{
    public partial class GameServer
    {
        private const int maxHomeLocations = 8;

#if DISPLAY_MAP
        private Size _mapSize;
        private Bitmap _mapBitmap;
        private Graphics _mapGraphics;
#endif

        // private functions

        private void ApplyPressure(Character character)
        {
            int race = (int)character.CharacterRace;
            MapHex hex = _map[character.CharacterLocationX + character.CharacterLocationY * _mapWidth];

            // gets the current and max score

            double score = character.ShipListBPV;
            double max;

            if (race <= (int)Races.kLastEmpire)
            {
                if ((character.State & Character.States.IsCpu) == Character.States.IsCpu)
                    score *= cpuPressureMultiplier;
                else
                    score *= humanPressureMultiplier;

                max = hex.EmpireBaseVictoryPoints;
            }
            else if (race <= (int)Races.kLastCartel)
            {
                Contract.Assert((character.State & Character.States.IsCpu) == Character.States.IsCpu);

                score *= cpuPressureMultiplier;
                max = hex.CartelBaseVictoryPoints;
            }
            else
            {
                Contract.Assert((character.State & Character.States.IsCpu) == Character.States.IsCpu);

                score *= (cpuPressureMultiplier * 0.5);
                max = (hex.EmpireBaseVictoryPoints + hex.CartelBaseVictoryPoints) * 0.5;
            }

            // updates the current score

            Contract.Assert(score > 0.0);

            score += hex.ControlPoints[race];

            Contract.Assert(score > 0.0);

            if (score <= max)
                hex.ControlPoints[race] = score;
            else
            {
                double surplus = score - max;
                double min = max * 0.25;

                // checks which layer we are applying pressing

                int allies = (int)_alliances[race];

                // everyone looses a bit of control in the hex where we have maxed out

                for (int i = 0; i < (int)Races.kNumberOfRaces; i++)
                {
                    if (i != race)
                    {
                        if (((1 << i) & allies) != 0)
                        {
                            if (hex.ControlPoints[i] > min)
                            {
                                hex.ControlPoints[i] -= surplus;

                                if (hex.ControlPoints[i] < min)
                                    hex.ControlPoints[i] = min;
                            }
                        }
                        else
                        {
                            if (hex.ControlPoints[i] > 0.0)
                            {
                                hex.ControlPoints[i] -= surplus;

                                if (hex.ControlPoints[i] < 0.0)
                                    hex.ControlPoints[i] = 0.0;
                            }
                        }
                    }
                }

                // and we keep max control over it

                hex.ControlPoints[race] = max;
            }
        }

        private void UpdateHexOwnership()
        {
            bool isDirty = false;

            for (int i = 0; i < _map.Length; i++)
            {
                MapHex hex = _map[i];

                int empire = (int)hex.EmpireControl;
                int cartel = (int)hex.CartelControl;

                double empirePoints = hex.ControlPoints[empire];
                double cartelPoints = hex.ControlPoints[cartel];

                for (int j = (int)Races.kFirstEmpire; j <= (int)Races.kLastEmpire; j++)
                {
                    if (empirePoints < hex.ControlPoints[j])
                    {
                        isDirty = true;

                        empire = j;
                        empirePoints = hex.ControlPoints[j];
                    }

                    int k = j + (int)Races.kFirstCartel;

                    if (cartelPoints < hex.ControlPoints[k])
                    {
                        isDirty = true;

                        cartel = k;
                        cartelPoints = hex.ControlPoints[k];
                    }
                }

                for (int j = (int)Races.kOrion; j <= (int)Races.kMonster; j++)
                {
                    if (empirePoints < hex.ControlPoints[j])
                    {
                        isDirty = true;

                        empire = (int)Races.kNeutralRace;
                        empirePoints = hex.ControlPoints[j];
                    }

                    if (cartelPoints < hex.ControlPoints[j])
                    {
                        isDirty = true;

                        cartel = (int)Races.kNeutralRace;
                        cartelPoints = hex.ControlPoints[j];
                    }
                }

                if (hex.EmpireControl != (Races)empire)
                {
                    hex.EmpireControl = (Races)empire;

                    Contract.Assert(hex.EmpireControl == Races.kNeutralRace || hex.EmpireControl >= Races.kFirstEmpire && hex.EmpireControl <= Races.kLastEmpire);

                    if (empire == (int)Races.kNeutralRace)
                        hex.EmpireCurrentVictoryPoints = 0;
                    else
                    {
                        hex.EmpireCurrentVictoryPoints = (int)Math.Round(hex.ControlPoints[empire], MidpointRounding.AwayFromZero);

                        if (hex.EmpireCurrentVictoryPoints > hex.EmpireBaseVictoryPoints)
                            hex.EmpireCurrentVictoryPoints = hex.EmpireBaseVictoryPoints;
                    }
                }

                if (hex.CartelControl != (Races)cartel)
                {
                    hex.CartelControl = (Races)cartel;

                    Contract.Assert(hex.CartelControl == Races.kNeutralRace || hex.CartelControl >= Races.kFirstCartel && hex.CartelControl <= Races.kLastCartel);

                    if (cartel == (int)Races.kNeutralRace)
                        hex.CartelCurrentVictoryPoints = 0;
                    else
                    {
                        hex.CartelCurrentVictoryPoints = (int)Math.Round(hex.ControlPoints[cartel], MidpointRounding.AwayFromZero);

                        if (hex.CartelCurrentVictoryPoints > hex.CartelBaseVictoryPoints)
                            hex.CartelCurrentVictoryPoints = hex.CartelBaseVictoryPoints;
                    }
                }
            }

            if (isDirty)
            {
                UpdateHexHomeStatus();
                //BroadcastHex(-1);  TODO

#if DISPLAY_MAP
                if ((_seconds & 31) == 0)
                    DisplayMap(0);
#endif

            }
        }

        private void UpdateHexHomeStatus()
        {
            // resets the home status

            for (int i = 0; i < _map.Length; i++)
            {
                MapHex hex = _map[i];

                hex.IsEmpireHome = false;
                hex.IsCartelHome = false;
            }

            // sets the new home status

            int[] coord = ArrayPool<int>.Shared.Rent(65356);

            for (int i = (int)Races.kFirstEmpire; i <= (int)Races.kLastCartel; i++)
            {
                Location[] locations = _homeLocations[i];

                for (int j = 0; j < maxHomeLocations && locations[j] != null; j++)
                {
                    MapHex h = _map[locations[j].X + locations[j].Y * _mapWidth];

                    if (i <= (int)Races.kLastEmpire)
                    {
                        if (h.IsEmpireHome)
                            continue;

                        h.IsEmpireHome = true;
                    }
                    else
                    {
                        if (h.IsCartelHome)
                            continue;

                        h.IsCartelHome = true;
                    }

                    coord[0] = locations[j].X;
                    coord[1] = locations[j].Y;

                    ExpandHexHomeStatus(coord, i, 2);
                }
            }

            ArrayPool<int>.Shared.Return(coord);
        }

        private void ExpandHexHomeStatus(int[] coord, int r, int c1)
        {
            int c2 = c1;

            for (int i = 0; i < c1; i += 2)
            {
                int[] locationIncrements = _locationIncrements[coord[i] & 1];
                int i1 = coord[i] + coord[i + 1] * _mapWidth;

                for (int j = 1; j < 7; j++)
                {
                    int i2 = i1 + locationIncrements[j];

                    if (!MovementValid(i1, i2))
                        continue;

                    MapHex h = _map[i2];

                    if (r <= (int)Races.kLastEmpire)
                    {
                        if (h.EmpireControl != (Races)r || h.IsEmpireHome)
                            continue;

                        h.IsEmpireHome = true;
                    }
                    else
                    {
                        if (h.CartelControl != (Races)r || h.IsCartelHome)
                            continue;

                        h.IsCartelHome = true;
                    }

                    coord[c2] = h.X;
                    coord[c2 + 1] = h.Y;

                    c2 += 2;
                }
            }

            int c = c2 - c1;

            if (c == 0)
                return;

            Buffer.BlockCopy(coord, c1, coord, 0, c);

            ExpandHexHomeStatus(coord, r, c);
        }

#if DISPLAY_MAP
        private void DisplayMap(int layer)
        {
            int i = 0;

            Console.Clear();

            for (int y = 0; y < _mapHeight; y++)
            {
                for (int x = 0; x < _mapWidth; x++)
                {
                    MapHex hex = _map[i];

                    i++;

                    ConsoleColor color;

                    switch (layer)
                    {
                        case 0:
                            if (hex.EmpireControl == Races.kNeutralRace)
                                color = ConsoleColor.Black;
                            else
                                color = _raceColors[(int)hex.EmpireControl];

                            break;

                        case 1:
                            if (hex.CartelControl == Races.kNeutralRace)
                                color = ConsoleColor.Black;
                            else
                                color = _raceColors[(int)hex.CartelControl - 8];

                            break;

                        default:
                            throw new NotImplementedException();
                    }

                    Console.ForegroundColor = color;

                    if ((y & 1) == 0)
                        Console.Write(' ');

                    if (hex.Planet > 0)
                        Console.Write('O');
                    else if (hex.Base > 0)
                        Console.Write('o');
                    else
                        Console.Write('#');

                    if ((y & 1) == 1)
                        Console.Write(' ');
                }

                Console.WriteLine();
            }

            Console.ResetColor();

            if (_mapGraphics == null)
            {
                _mapSize = new(580, 516);
                _mapBitmap = new(_mapSize.Width, _mapSize.Height, PixelFormat.Format24bppRgb);
                _mapGraphics = Graphics.FromImage(_mapBitmap);
            }

            _mapGraphics.CopyFromScreen(8, 32, 0, 0, _mapSize);
            _mapBitmap.Save("C:/Users/Carlos Santos/Pictures/001/" + _seconds + ".jpg", ImageFormat.Jpeg);
        }
#endif

        // home locations

        private void UpdateHomeLocations()
        {
            for (int i = 0; i < (int)Races.kNumberOfRaces; i++)
            {
                for (int j = 0; j < maxHomeLocations; j++)
                    _homeLocations[i][j] = null;
            }

            for (int i = 0; i < _map.Length; i++)
            {
                MapHex hex = _map[i];

                int control = 0; // neutral

                if (hex.EmpireControl != Races.kNeutralRace) control += 1;
                if (hex.CartelControl != Races.kNeutralRace) control += 2;

                foreach (KeyValuePair<int, object> p in hex.Population)
                {
                    Character character = _characters[p.Key];

                    int c = character.ShipCount;

                    for (int j = 0; j < c; j++)
                    {
                        Ship ship = character.GetShipAt(j);

                        Contract.Assert(_ships.ContainsKey(ship.Id));

                        // tries to assign a score

                        int score;

                        if (ship.ClassType == ClassTypes.kClassPlanets)
                        {
                            //Contract.Assert(hex.Planet > 0);

                            score = (63 - hex.Planet << 16) + ship.BPV;
                        }
                        else if (ship.ClassType >= ClassTypes.kClassListeningPost && ship.ClassType <= ClassTypes.kClassStarBase)
                        {
                            //Contract.Assert(hex.Base > 0);

                            score = (31 - hex.Base << 16) + ship.BPV;
                        }
                        else
                            continue;

                        // sorts the locations

                        if (control == 0)
                            SortLocations(hex, score, _homeLocations[(int)Races.kNeutralRace]);

                        if ((control & 1) == 1 && hex.EmpireControl == character.CharacterRace)
                            SortLocations(hex, score, _homeLocations[(int)hex.EmpireControl]);

                        if ((control & 2) == 2)
                            SortLocations(hex, score, _homeLocations[(int)hex.CartelControl]);
                    }
                }
            }
        }

        private static void SortLocations(MapHex hex, int score, Location[] locations)
        {
            for (int k = 0; k < maxHomeLocations; k++)
            {
                if (locations[k] != null)
                {
                    if (locations[k].Z > score)
                        continue;

                    for (int l = maxHomeLocations - 1; l > k; l--)
                        locations[l] = locations[l - 1];
                }

                locations[k] = new Location(hex.X, hex.Y, score);

                break;
            }
        }

        // population census

        private static void ClearPopulationCensus(PopulationCensus census)
        {
            Array.Clear(census.RaceCount);
            Array.Clear(census.RaceBPV);

            Array.Clear(census.AllyCount);
            Array.Clear(census.AllyBPV);

            Array.Clear(census.EnemyCount);
            Array.Clear(census.EnemyBPV);

            Array.Clear(census.NeutralCount);
            Array.Clear(census.NeutralBPV);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AdjustPopulationCensus(Character character, int bpv)
        {
            MapHex hex = _map[character.CharacterLocationX + character.CharacterLocationY * _mapWidth];

            AdjustPopulationCensus(hex.Census, (int)character.CharacterRace, Math.Sign(bpv), bpv);
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void AdjustPopulationCensus(PopulationCensus census, int race, int count, int bpv)
        {
            Contract.Assert(count != 0 && bpv != 0);

            census.RaceCount[race] += count;
            census.RaceBPV[race] += bpv;

            Contract.Assert(census.RaceCount[race] >= 0 && census.RaceBPV[race] >= 0);

            _census.RaceCount[race] += count;
            _census.RaceBPV[race] += bpv;

            int i = 0;
            RaceMasks mask = (RaceMasks)(1 << race);

            if (race <= (int)Races.kLastCartel)
            {
                for (; i <= (int)Races.kLastCartel; i++)
                {
                    if (i != race)
                    {
                        if ((_alliances[i] & mask) != RaceMasks.None)
                        {
                            census.AllyCount[i] += count;
                            census.AllyBPV[i] += bpv;

                            Contract.Assert(census.AllyCount[i] >= 0 && census.AllyBPV[i] >= 0);

                            _census.AllyCount[i] += count;
                            _census.AllyBPV[i] += bpv;
                        }
                        else
                        {
                            census.EnemyCount[i] += count;
                            census.EnemyBPV[i] += bpv;

                            Contract.Assert(census.EnemyCount[i] >= 0 && census.EnemyBPV[i] >= 0);

                            _census.EnemyCount[i] += count;
                            _census.EnemyBPV[i] += bpv;
                        }
                    }
                }

                for (; i < (int)Races.kNumberOfRaces; i++)
                {
                    Contract.Assert((_alliances[i] & mask) == RaceMasks.None);

                    census.NeutralCount[i] += count;
                    census.NeutralBPV[i] += bpv;

                    Contract.Assert(census.NeutralCount[i] >= 0 && census.NeutralBPV[i] >= 0);

                    _census.NeutralCount[i] += count;
                    _census.NeutralBPV[i] += bpv;
                }
            }
            else
            {
                for (; i <= (int)Races.kLastCartel; i++)
                {
                    Contract.Assert((_alliances[i] & mask) == RaceMasks.None);

                    census.EnemyCount[i] += count;
                    census.EnemyBPV[i] += bpv;

                    Contract.Assert(census.EnemyCount[i] >= 0 && census.EnemyBPV[i] >= 0);

                    _census.EnemyCount[i] += count;
                    _census.EnemyBPV[i] += bpv;
                }

                for (; i < (int)Races.kNumberOfRaces; i++)
                {
                    if (i != race)
                    {
                        Contract.Assert((_alliances[i] & mask) != RaceMasks.None);

                        census.AllyCount[i] += count;
                        census.AllyBPV[i] += bpv;

                        Contract.Assert(census.AllyCount[i] >= 0 && census.AllyBPV[i] >= 0);

                        _census.AllyCount[i] += count;
                        _census.AllyBPV[i] += bpv;
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void GetPopulationCensus(PopulationCensus census, int characterRace, int characterShipCount, int characterShipsBPV, int factors, out int count, out int bpv)
        {
            count = characterShipCount;

            if ((factors & 0x01) != 0)
                count += census.RaceCount[characterRace];

            if ((factors & 0x02) != 0)
                count += census.AllyCount[characterRace];

            if ((factors & 0x04) != 0)
                count += census.EnemyCount[characterRace];

            if ((factors & 0x08) != 0)
                count += census.NeutralCount[characterRace];

            Contract.Assert(count >= 0);

            bpv = characterShipsBPV;

            if ((factors & 0x10) != 0)
                bpv += census.RaceBPV[characterRace];

            if ((factors & 0x20) != 0)
                bpv += census.AllyBPV[characterRace];

            if ((factors & 0x40) != 0)
                bpv += census.EnemyBPV[characterRace];

            if ((factors & 0x80) != 0)
                bpv += census.NeutralBPV[characterRace];

            Contract.Assert(bpv >= 0);
        }
    }
}
