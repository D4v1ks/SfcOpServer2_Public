using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

namespace SfcOpServer
{
    public partial class GameServer
    {
        // human

        private void BeginHumanMovement(Character character, int destinationX, int destinationY)
        {
            Contract.Assert((character.State & Character.States.IsBusy) != Character.States.IsBusy);

            character.State |= Character.States.IsBusy;

            Contract.Assert(character.MoveDestinationX == -1 && character.MoveDestinationY == -1);

            character.MoveDestinationX = destinationX;
            character.MoveDestinationY = destinationY;

            // tries to get a mission

            if (character.Client.LauncherId != 0)
                TryGetMission(character);
        }

        private void ContinueHumanMovement(Character character)
        {
            Contract.Assert(character.MoveDestinationX != -1 && character.MoveDestinationY != -1);

            RemoveFromHexPopulation(character);
            AddHexRequests(character);

            character.CharacterLocationX = character.MoveDestinationX;
            character.CharacterLocationY = character.MoveDestinationY;

            character.MoveDestinationX = -1;
            character.MoveDestinationY = -1;

            AddToHexPopulation(character);
            AddHexRequests(character);

            ApplyPressure(character);

            // adds the movement to the list

            long ticks = (_humanMovementDelay * 1000) + Environment.TickCount64;

            if (!_humanMovements.TryAdd(character.Id, ticks))
                throw new NotSupportedException();
        }

        private void ProcessHumanMovements(long t0, Queue<int> queuedMoves)
        {
            if (_humanMovements.Count == 0)
                return;

            Contract.Assert(queuedMoves.Count == 0);

            foreach (KeyValuePair<int, long> p in _humanMovements)
            {
                int characterId = p.Key;

                if (p.Value > t0)
                    continue;

                Character character = _characters[characterId];

                Contract.Assert((character.State & Character.States.IsBusy) == Character.States.IsBusy && character.MoveDestinationX == -1 && character.MoveDestinationY == -1);

                Client27000 client = character.Client;

                // updates the icons

                Write(client, ClientRequests.MetaViewPortHandlerNameC_0x06_0x00_0x0f, 0x00); // 14_F

                client.IconRequest = false;

                // finishes the movement

                Clear();

                Push(0x00);
                Push(0x00);
                Push(character.CharacterLocationY);
                Push(character.CharacterLocationX);
                Push(character.Id);
                Push(0x00);
                Push(client.Id, client.Relays[(int)ClientRelays.MetaViewPortHandlerNameC], 0x04);

                Push(client, ClientRequests.PlayerRelayC_0x03_0x00_0x04, character.Id); // 14_10

                Write(client);

                // tries to update the missions

                Clear();

                if (character.Mission != 0)
                {
                    if ((character.Mission & HostMask) >> HostShift == character.Id)
                        PushHostMissions(client);
                    else
                    {
                        int draftId = character.CharacterLocationX + character.CharacterLocationY * _mapWidth;

                        if (_drafts.TryGetValue(draftId, out Draft draft))
                        {
                            if (draft.Mission == null)
                            {
                                draft.Expected.Add(character.Id, null);

                                PushGuestMissions(client);
                            }
                        }
                    }
                }

                TryWrite(client);

                // tries to update the hexes

                TryWriteHexRequests(client);

                // releases the character

                Contract.Assert((character.State & Character.States.IsBusy) == Character.States.IsBusy);

                if (character.State != Character.States.IsHumanBusyDisconnecting)
                    character.State &= ~Character.States.IsBusy;

                // removes the movement from the list

                queuedMoves.Enqueue(character.Id);
            }

            int c = queuedMoves.Count;

            if (c != 0)
            {
                do
                {
                    _humanMovements.Remove(queuedMoves.Dequeue());

                    c--;
                }
                while (c != 0);
            }
        }

        // AI

        private void AddOrUpdateCpuMovement(long t0, Character character)
        {
            long ticks = t0 + _rand.NextInt32(_cpuMovementMinRest, _cpuMovementMaxRest) * 1000;

            if (!_cpuMovements.TryAdd(character.Id, ticks))
                _cpuMovements[character.Id] = ticks;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void ProcessCpuMovements(long t0, List<int> list1, List<int> list2)
        {
            Contract.Assert(list1.Count == 0 && list2.Count == 0);

            foreach (KeyValuePair<int, long> p in _cpuMovements)
            {
                Character character = _characters[p.Key];

                // checks if the cpu is playing a mission

                if (character.State == Character.States.IsCpuAfkBusyOnline)
                {
                    Contract.Assert(character.Mission != 0);

                    continue;
                }

                Contract.Assert(character.State == Character.States.IsCpuOnline);

                // checks if the cpu is resting

                if (p.Value > t0)
                    continue;

                // decides what to do

                int[] locationIncrements = _locationIncrements[character.CharacterLocationX & 1];
                int i1 = character.CharacterLocationX + character.CharacterLocationY * _mapWidth;
                int i2;

                int race = (int)character.CharacterRace;

                MapHex destination;

                if (((1 << (int)character.BestShipClass) & ClassTypeIconMask) != 0)
                {
                    MapHex origin = _map[i1];

                    int shipListCount = character.ShipCount;
                    int shipListBPV = character.ShipListBPV;

                    int factors = 0x04ff;

                    GetPopulationCensus(origin.Census, race, -shipListCount, -shipListBPV, factors, out int count1, out int bpv1);

                    Location[] locations = _homeLocations[race];

                    bool isHomeless = locations[0] == null;
                    bool isNearHome = false;

                    int bestCriteria = int.MaxValue;

                    int bestDistance = int.MaxValue;
                    int bestDestination = -1;

                    for (int i = 1; i < 7; i++)
                    {
                        i2 = i1 + locationIncrements[i];

                        if (!MovementValid(i1, i2))
                            continue;

                        destination = _map[i2];

                        // checks if the hex is 'out of bounds' (has max impendence) or is locked in a mission 

                        if (destination.BaseSpeedPoints >= 2.0 || (destination.Mission & IsClosedMask) == IsClosedMask)
                            continue;

                        // characters without home, or neutral characters, just move randomly around the map

                        if (isHomeless || (_raceList & (1u << race)) == 0u)
                        {
                            list1.Add(i2);

                            continue;
                        }

                        // the other characters try move to, or from, a hex in their home territory

                        int j = 0;

                        if ((int)origin.EmpireControl == race && origin.IsEmpireHome) j += 1;
                        if ((int)origin.CartelControl == race && origin.IsCartelHome) j += 2;

                        if ((int)destination.EmpireControl == race && destination.IsEmpireHome) j += 4;
                        if ((int)destination.CartelControl == race && destination.IsCartelHome) j += 8;

                        if ((race <= (int)Races.kLastEmpire && (j & 5) != 0) || (race >= (int)Races.kFirstCartel && (j & 10) != 0))
                        {
                            isNearHome = true;

                            const int minPopulation = -1;
                            const int maxPopulation = +1;

                            const int minBPV = -200;
                            const int maxBPV = +300;

                            GetPopulationCensus(destination.Census, race, shipListCount, shipListBPV, factors, out int count2, out int bpv2);

                            bool matchCriteria;

                            j = factors & 0x0f00;

                            if (j == 0x0100)
                            {
                                j = count1 - count2;

                                matchCriteria = (j >= minPopulation && j <= maxPopulation);
                            }
                            else if (j == 0x0200)
                            {
                                j = bpv1 - bpv2;

                                matchCriteria = j >= minBPV && j <= maxBPV;
                            }
                            else if (j == 0x0400)
                            {
                                if (count1 == 0)
                                    j = -bpv2 / count2;
                                else if (count2 == 0)
                                    j = bpv1 / count1;
                                else
                                    j = (bpv1 / count1) - (bpv2 / count2);

                                matchCriteria = (j >= minBPV && j <= maxBPV);
                            }
                            else
                                matchCriteria = false;

                            if (matchCriteria)
                            {
                                list1.Add(i2);

                                continue;
                            }

                            if (list1.Count > 0)
                                continue;

                            j = Math.Abs(j);

                            if (bestCriteria > j)
                            {
                                bestCriteria = j;

                                list2.Clear();
                                list2.Add(i2);
                            }
                            else if (bestCriteria == j)
                                list2.Add(i2);
                        }
                        else
                        {
                            // if not, then they try to move back home

                            Contract.Assert(!isHomeless);

                            for (j = 0; j < maxHomeLocations; j++)
                            {
                                if (locations[j] != null)
                                {
                                    int x = locations[j].X - destination.X;
                                    int y = locations[j].Y - destination.Y;

                                    int d = x * x + y * y;

                                    if (bestDistance > d)
                                    {
                                        bestDistance = d;
                                        bestDestination = i2;
                                    }
                                }
                            }
                        }
                    }

                    if (list1.Count > 0)
                        i2 = list1[_rand.NextInt32(list1.Count)];
                    else if (list2.Count > 0)
                        i2 = list2[_rand.NextInt32(list2.Count)];
                    else if (bestDestination != -1)
                        i2 = bestDestination;
                    else if (isNearHome)
                        goto applyPressure;
                    else
                        goto updateCpuMovement;

                    destination = _map[i2];

                    RemoveFromHexPopulation(origin, character);

                    character.CharacterLocationX = destination.X;
                    character.CharacterLocationY = destination.Y;

                    AddToHexPopulation(destination, character);

                    list1.Clear();
                    list2.Clear();
                }
                else
                {
                    // we need to check if our hex, or at least one of the adjacent hexes, is owned by us or not
                    // we do this to prevent the exploitation of remote bases\planets in order to advance our empire

                    for (int i = 0; i < 7; i++)
                    {
                        i2 = i1 + locationIncrements[i];

                        if (!MovementValid(i1, i2))
                            continue;

                        destination = _map[i2];

                        if ((int)destination.EmpireControl == race || (int)destination.CartelControl == race)
                            goto applyPressure;
                    }

                    goto updateCpuMovement;
                }

            applyPressure:

                ApplyPressure(character);

            updateCpuMovement:

                AddOrUpdateCpuMovement(t0, character);
            }
        }

        // common

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool MovementValid(int i1, int i2)
        {
            return (i2 >= 0) & (i2 < _map.Length) & (Math.Abs(i1 % _mapWidth - i2 % _mapWidth) <= 2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool MovementValid(int x1, int y1, int x2, int y2)
        {
            Contract.Assert(x2 != -1 && y2 != -1);

            int x3 = Math.Abs(x1 - x2);
            int y3 = Math.Abs(y1 - y2);

            return x3 + y3 == 1 || ((x3 | y3) == 1 && (((x1 & 1) == 0 && y1 > y2) || ((x1 & 1) != 0 && y1 < y2)));
        }
    }
}
