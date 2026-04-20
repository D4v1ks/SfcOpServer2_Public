using shrNet;

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace SfcOpServer
{
    public unsafe partial class GameServer
    {
        private void InitializeStack()
        {
            _stack = new byte[Client27000.MaximumBufferSize];
            _handle = GCHandle.Alloc(_stack, GCHandleType.Pinned);
            _end = (byte*)_handle.AddrOfPinnedObject() + Client27000.MaximumBufferSize;

            Clear();
        }

        private void DisposeStack()
        {
            _handle.Free();
        }

        // main operations

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Clear()
        {
            _head = _end;
            _tail = _end;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Flush()
        {
            _tail = _head;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Write(Client27000 client)
        {
            long length = _end - _head;

            Contract.Assert(length != 0);

            client.TryWrite(new Span<byte>(_head, (int)length));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void TryWrite(Client27000 client)
        {
            long length = _end - _head;

            if (length != 0)
                client.TryWrite(new Span<byte>(_head, (int)length));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Write(Client27001 launcher)
        {
            _head -= 4;

            long length = _end - _head;

            *(int*)_head = (int)length;

            launcher.TryWrite(new Span<byte>(_head, (int)length));
        }

        // simple operations

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Push(byte value)
        {
            _head--;

            *_head = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Push(short value)
        {
            _head -= 2;

            *(short*)_head = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Push(int value)
        {
            _head -= 4;

            *(int*)_head = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Push(float value)
        {
            _head -= 4;

            *(float*)_head = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Push(double value)
        {
            _head -= 8;

            *(double*)_head = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Push(byte[] value)
        {
            long length = value.Length;

            Contract.Assert(length != 0);

            fixed (byte* bytes = value)
            {
                _head -= length;

                Buffer.MemoryCopy(bytes, _head, length, length);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Push(string value)
        {
            long length = value.Length;

            if (length != 0)
            {
                fixed (char* chars = value)
                {
                    _head -= length;

                    Encoding.UTF8.GetBytes(chars, (int)length, _head, (int)length);
                }
            }

            _head -= 4;

            *(int*)_head = (int)length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Push(Client27000 client, ClientRequests request, int info)
        {
            int[] requests = client.Requests[(int)request];

            Contract.Assert(client.Relays[requests[0]] != -1 && _tail - _head == 0);

            _head -= 30;

            *(int*)_head = 30;

            _head[4] = 0;

            *(int*)(_head + 5) = client.Id;
            *(int*)(_head + 9) = client.Relays[requests[0]];
            *(int*)(_head + 13) = requests[1];

            *(int*)(_head + 17) = 9;

            *(int*)(_head + 21) = requests[2];
            _head[25] = (byte)requests[3];
            *(int*)(_head + 26) = info;

            _tail = _head;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Push(int i1, int i2, int i3)
        {
            long length = _tail - _head;

            Contract.Assert(length > 0 && length <= (Client27000.MaximumBufferSize - 21));

            _head -= 21;

            *(int*)_head = (int)length + 21;

            _head[4] = 0;

            *(int*)(_head + 5) = i1;
            *(int*)(_head + 9) = i2;
            *(int*)(_head + 13) = i3;

            *(int*)(_head + 17) = (int)length;

            _tail = _head;
        }

        // composed operations

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void Push(Character character, bool isHexCacheSet, bool isFlagSet)
        {
            Push(0x00);

            if (isFlagSet)
                Push((byte)0x01);
            else
                Push((byte)0x00);

            if (isHexCacheSet)
                PushHexCache(_map[character.CharacterLocationX + character.CharacterLocationY * _mapWidth], 0);
            else
                PushHexCache(null, 0);

            for (int i = character.ShipCount - 1; i >= 0; i--)
                PushShipCache(character.GetShipAt(i));

            Push(character.ShipCount);
            Push(character.MoveDestinationY);
            Push(character.MoveDestinationX);
            Push(character.HomeWorldLocationY);
            Push(character.HomeWorldLocationX);
            Push(character.CharacterLocationY);
            Push(character.CharacterLocationX);

            Contract.Assert(character.Unknown == 0);

            Push(character.Unknown); // undefined string ?

            Push(character.CharacterLifetimePrestige);
            Push(character.CharacterCurrentPrestige);
            Push(character.CharacterRating);
            Push((int)character.CharacterRank);

            // the political control defines the access to the shipyard and supplies in the client interface
            // (by default empires are restricted to their planets and bases, but cartels can have access to any planet or base in the map)

            Contract.Assert(character.CharacterPoliticalControl == character.CharacterRace);

            Push((int)character.CharacterPoliticalControl);
            Push((int)character.CharacterRace);
            Push(character.CharacterName);
            Push(character.Id);
            Push(character.WONLogon);
            Push(character.IPAddress);
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void PushHexCache(MapHex hex, int lockID)
        {
            if (hex != null)
            {
                Contract.Assert(lockID == 0 || lockID == 1);

                Push(hex.CurrentSpeedPoints);
                Push(hex.BaseSpeedPoints);

                Push(hex.CartelCurrentVictoryPoints);
                Push(0x01);
                Push(hex.EmpireCurrentVictoryPoints);
                Push(0x00);

                Push(0x02); // counter 3

                Push(hex.CartelBaseVictoryPoints);
                Push(0x01);
                Push(hex.EmpireBaseVictoryPoints);
                Push(0x00);

                Push(0x02); // counter 2

                Push(hex.CurrentEconomicPoints);
                Push(hex.BaseEconomicPoints);
                Push((int)hex.BaseType);
                Push((int)hex.PlanetType);
                Push((int)hex.TerrainType);

                Push((int)hex.CartelControl);
                Push(0x01);
                Push((int)hex.EmpireControl);
                Push(0x00);

                Push(0x02); // counter 1

                Push(hex.Y);
                Push(hex.X);
                Push(lockID);
                Push(hex.Id);
            }
            else
            {
                Contract.Assert(lockID == 0);

                Push(0.0); // hex.CurrentSpeedPoints
                Push(0.0); // hex.BaseSpeedPoints

                Push(0x00); // counter 3

                Push(0x00); // counter 2

                Push(0x00); // hex.CurrentEconomicPoints
                Push(0x00); // hex.BaseEconomicPoints
                Push(0x00); // hex.BaseType
                Push(0x00); // hex.PlanetType
                Push(0x00); // hex.TerrainType

                Push(0x00); // counter 1

                Push(0x00); // hex.Y
                Push(0x00); // hex.X
                Push(0x00); // lockID
                Push(0x00); // hex.Id
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void PushShipCache(Ship ship)
        {
            float epvRatio = (float)ship.EPV / ship.BPV;

            Contract.Assert(epvRatio == 1f);

            Push(0x00);
            Push(ship.Name);
            Push(epvRatio);
            Push(ship.ShipClassName);
            Push((int)ship.ClassType);
            Push(ship.BPV);
            Push(ship.Id);
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void Push(Character character, BidItem item)
        {
            if (item.BiddingHasBegun == 0)
            {
                Push(0x00); // item.BidMaximum
                Push(0x00); // item.TurnBidMade
                Push(0x00); // item.BidOwnerID
                Push(item.CurrentBid);
                Push(0x00); // item.TurnToClose
                Push(0x00); // item.TurnOpened
                Push(item.AuctionRate);
                Push(item.AuctionValue);
                Push(item.ShipBPV);
                Push(item.ShipId);
                Push(item.ShipClassName);
                Push((byte)0x00); // item.BiddingHasBegun
                Push(0x00); // item.LockID
                Push(0x00); // item.Id
            }
            else
            {
                Contract.Assert(item.BiddingHasBegun == 1);

                if (character.Id == item.BidOwnerID)
                    Push(item.BidMaximum);
                else
                    Push(0x00);

                Push(item.TurnBidMade);
                Push(item.BidOwnerID);
                Push(item.CurrentBid);
                Push(item.TurnToClose);
                Push(item.TurnOpened);
                Push(item.AuctionRate);
                Push(item.AuctionValue);
                Push(item.ShipBPV);
                Push(item.ShipId);
                Push(item.ShipClassName);
                Push((byte)0x01); // item.BiddingHasBegun 
                Push(item.LockID);
                Push(item.Id);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void Push(Ship ship)
        {
            Push(0x00);

            // officers

            for (int i = (int)OfficerTypes.kMaxOfficers - 1; i >= 0; i--)
            {
                ref Officer officer = ref ship.Officers.Items[i];

                // only the officer rank is sent to the server

                Push(0x00);
                Push(0x00);
                Push((int)officer.Rank);
                Push(0x00);

                /*
                    Push(officer.Unknown2);
                    Push(officer.Unknown1);
                    Push((int)officer.Rank);
                    Push(officer.Name);
                */
            }

            // stores

            ShipStores stores = ship.Stores;

            for (int i = 3; i >= 0; i--)
            {
                ref FighterBay fighterBay = ref stores.FighterBays[i];

                Contract.Assert(fighterBay.Unknown2 == 0);

                Push(fighterBay.Unknown2);
                Push(fighterBay.FighterType);

                Push(fighterBay.Unknown1);
                Push(fighterBay.FightersMax);
                Push(fighterBay.FightersLoaded);
                Push(fighterBay.FightersCount);
            }

            Push(stores.DamageControl.CurrentQuantity);
            Push(stores.DamageControl.BaseQuantity);
            Push(stores.DamageControl.MaxQuantity);

            Push(stores.TBombs.CurrentQuantity);
            Push(stores.TBombs.BaseQuantity);
            Push(stores.TBombs.MaxQuantity);

            Push(stores.BoardingParties.CurrentQuantity);
            Push(stores.BoardingParties.BaseQuantity);
            Push(stores.BoardingParties.MaxQuantity);

            // only uninitialized weapon hardpoints are sent to the server

            for (int i = 24; i >= 0; i--)
            {
                Push((short)WeaponArcs.Uninitialized);
                Push((short)WeaponStates.Uninitialized);

                /*
                    Push((short)stores.WeaponHardpoints[i].Arc);
                    Push((short)stores.WeaponHardpoints[i].State);
                */
            }

            // transport items are not sent to the server

            Push(0x00); // no transport items

            /*
                if (setupTeamId == TeamIds.kNoTeam)
                {
                    Contract.Assert(currentTeamId == TeamIds.kNoTeam);
            
                    Push(0x00); // no transport items
                }
                else
                {
                    Contract.Assert(!stores.TransportItems.ContainsKey(TransportItems.kTransSpareParts));

                    if (stores.DamageControl.CurrentQuantity > 0)
                        stores.TransportItems.Add(TransportItems.kTransSpareParts, 1);

                    foreach (KeyValuePair<TransportItems, int> p in stores.TransportItems)
                    {
                        Contract.Assert(p.Key > TransportItems.kTransNothing && p.Key < TransportItems.Total && p.Value > 0 && p.Value < 256);

                        Push(0x01); // p.Value
                        Push((int)p.Key);
                    }

                    Push(stores.TransportItems.Count);
                }
            */

            Push(stores.Unknown5.CurrentQuantity);
            Push(stores.Unknown5.BaseQuantity);
            Push(stores.Unknown5.MaxQuantity);

            Push(stores.Unknown4.CurrentQuantity);
            Push(stores.Unknown4.BaseQuantity);
            Push(stores.Unknown4.MaxQuantity);

            Push(stores.Unknown3.CurrentQuantity);
            Push(stores.Unknown3.BaseQuantity);
            Push(stores.Unknown3.MaxQuantity);

            Push(stores.General.CurrentQuantity);
            Push(stores.General.BaseQuantity);
            Push(stores.General.MaxQuantity);

            for (int i = 24; i >= 0; i--)
            {
                ref MissileHardpoint missileHardpoint = ref stores.MissileHardpoints[i];

                Push(missileHardpoint.TubesCapacity);
                Push(missileHardpoint.TubesCount);
                Push(missileHardpoint.MissilesStored);
                Push(missileHardpoint.MissilesReady);
            }

            Push(stores.TotalMissilesStored);
            Push(stores.TotalMissilesReady);
            Push(stores.TotalMissilesReadyAndStored);
            Push(stores.TotalTubesCount);

            Push(stores.MissilesReloads);
            Push((byte)stores.MissilesDriveSystem);
            Push((byte)stores.MissilesType);

            Push((byte)0x01);
            Push((byte)0x01);

            // damage

            Push(ship.Systems.Items);

            Push(ship.TurnCreated);
            Push(ship.Name);
            Push(ship.ShipClassName);
            Push(ship.EPV);
            Push(ship.BPV);
            Push((int)ship.ClassType);
            Push((int)ship.Race);
            Push(ship.IsInAuction);
            Push(ship.OwnerID);
            Push(ship.LockID);
            Push(ship.Id);
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void Push(MapHex hex)
        {
            Contract.Assert(hex != null);

            Push((byte)(hex.CurrentSpeedPoints * 100.0));

            Push((byte)hex.CurrentEconomicPoints);

            Push((byte)hex.CartelCurrentVictoryPoints);
            Push((byte)hex.EmpireCurrentVictoryPoints);

            Push((byte)hex.Base);
            Push((byte)hex.Planet);
            Push((int)hex.TerrainType);

            Push((byte)hex.CartelControl);
            Push((byte)hex.EmpireControl);
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private static void FilterIcons(int bestId, int bestBPV, Span<int> list, ref int count)
        {
            for (int i = 0; i <= 2; i++)
            {
                if (list[1] < bestBPV)
                {
                    list[..6].CopyTo(list[2..]);

                    list[0] = bestId;
                    list[1] = bestBPV;

                    count++;

                    break;
                }

                list = list[2..];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private static void PushIds(Span<int> list, int count, ref Span<int> ids)
        {
            do
            {
                ids[0] = list[0];

                ids = ids[1..];
                list = list[2..];

                count--;
            }
            while (count > 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void PushStardate()
        {
            Push(_advancedYears);
            Push(_lateYears);
            Push(_middleYears);
            Push(_earlyYears);

            Push(_baseYear);

            Push(_millisecondsPerTurn);
            Push(_turnsPerYear);

            Push(0x00); // undefined string ?

            Push(_turn);
        }

        // complete operations

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void Write(Client27000 client, ClientRequests request, int info)
        {
            // sends the client request

            Clear();

            Push(client, request, info);

            Write(client);
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void TryWriteIconRequests(Client27000 client)
        {
            if (client.IconRequest)
            {
                Write(client, ClientRequests.MetaViewPortHandlerNameC_0x06_0x00_0x0f, 0x00); // 14_F

                client.IconRequest = false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void TryWriteHexRequests(Client27000 client)
        {
            if (client.HexRequests.Count != 0)
            {
                Clear();

                foreach (KeyValuePair<int, int> request in client.HexRequests)
                    Push(client, ClientRequests.MetaViewPortHandlerNameC_0x03_0x00_0x00, request.Key); // D_3

                Write(client);

                client.HexRequests.Clear();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void WritePing(Client27000 client)
        {
            Clear();

            Push(0x00);
            Push(0x00);
            Push(-1, 0, 4);

            Write(client);
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void WriteTurn(Client27000 client)
        {
            // 3a000000 00 020000000e00000001000000 25000000 1200000038f8af036d010000c0270900d7080000000000000a000000140000002800000000

            Clear();

            Push((byte)0x00);
            PushStardate();
            Push(client.Id, client.Relays[(int)ClientRelays.MetaViewPortHandlerNameC], 0x01);

            Write(client);

            // 3a000000 00 020000000900000001000000 25000000 1200000038f8af036d010000c0270900d7080000000000000a000000140000002800000000

            Clear();

            Push((byte)0x00);
            PushStardate();
            Push(client.Id, client.Relays[(int)ClientRelays.PlayerInfoPanel], 0x01);

            Write(client);
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void AddHexRequests(Character character)
        {
            int i1 = character.CharacterLocationX + character.CharacterLocationY * _mapWidth;
            int[] locationIncrements = _locationIncrements[character.CharacterLocationX & 1];
            Dictionary<int, int> hexRequests = character.Client.HexRequests;

            for (int i = 0; i < 7; i++)
            {
                int i2 = i1 + locationIncrements[i];

                if (MovementValid(i1, i2))
                {
                    int location = (i2 % _mapWidth << 16) + (i2 / _mapWidth);

                    if (!hexRequests.TryGetValue(location, out int flag))
                        hexRequests.Add(location, 1);
                    else if (flag == 1)
                        hexRequests.Remove(location);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void BroadcastIcons(int hexX, int hexY)
        {
            int i3 = hexX + hexY * _mapWidth;

            foreach (KeyValuePair<int, Client27000> p in _clients)
            {
                Client27000 client = p.Value;

                if ((client.State == Client27000.States.IsOnline) && (!client.IconRequest))
                {
                    Character character = client.Character;

                    int i1 = character.CharacterLocationX + character.CharacterLocationY * _mapWidth;
                    int[] locationIncrements = _locationIncrements[character.CharacterLocationX & 1];

                    for (int i = 0; i < 7; i++)
                    {
                        int i2 = i1 + locationIncrements[i];

                        if (i2 == i3 && MovementValid(i1, i2))
                        {
                            client.IconRequest = true;

                            break;
                        }
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void BroadcastHexAndIcons(int hexX, int hexY)
        {
            int i3 = hexX + hexY * _mapWidth;
            int location = (hexX << 16) + hexY;

            foreach (KeyValuePair<int, Client27000> p in _clients)
            {
                Client27000 client = p.Value;

                if ((client.State == Client27000.States.IsOnline) && (!client.IconRequest || !client.HexRequests.ContainsKey(location)))
                {
                    Character character = client.Character;

                    int i1 = character.CharacterLocationX + character.CharacterLocationY * _mapWidth;
                    int[] locationIncrements = _locationIncrements[character.CharacterLocationX & 1];

                    for (int i = 0; i < 7; i++)
                    {
                        int i2 = i1 + locationIncrements[i];

                        if (i2 == i3 && MovementValid(i1, i2))
                        {
                            client.IconRequest = true;
                            client.HexRequests.TryAdd(location, 0);

                            break;
                        }
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void ProcessClientLogouts()
        {
            while (_logouts.TryDequeue(out int clientId))
                LogoutClient(clientId);
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void ProcessClientMessages(long t)
        {
            foreach (KeyValuePair<int, Client27000> p in _clients)
            {
                Client27000 client = p.Value;

                while (client.TryRead(out DuplexMessage message))
                {
                    try
                    {
                        Process(client, message.Buffer, message.Length);

                        client.LastActivity = t;
                    }
                    catch (Exception e)
                    {
                        LogError("ProcessClientMessages()", e);
                    }
                    finally
                    {
                        message.Release();
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void ProcessClientRequests(Queue<int> queueInt)
        {
            long t = Environment.TickCount64;

            foreach (KeyValuePair<int, Client27000> p in _clients)
            {
                Client27000 client = p.Value;

                if (client.State == Client27000.States.IsOnline)
                {
                    if (t - client.LastActivity >= 60_000)
                    {
                        if (client.LastPingRequest < client.LastPingReply)
                        {
                            client.LastActivity = t + 30_000; // delays the next ping check
                            client.LastPingRequest = t;

                            WritePing(client);
                        }
                        else
                            client.Dispose();

                        continue;
                    }

                    if (client.LastTurn < _turn)
                    {
                        client.LastTurn++;

                        WriteTurn(client);

                        continue;
                    }

                    Character character = client.Character;

                    if ((character.State & (Character.States.IsAfk | Character.States.IsBusy)) == Character.States.None)
                    {
                        // checks the logins and logouts

                        Dictionary<int, object> idList = client.IdList;

                        if (idList.Count != _clients.Count)
                        {
                            Clear();

                            foreach (KeyValuePair<int, Client27000> q in _clients)
                            {
                                Client27000 otherClient = q.Value;

                                if (otherClient.State == Client27000.States.IsOnline)
                                {
                                    Character otherCharacter = otherClient.Character;

                                    if (idList.TryAdd(otherCharacter.Id, null))
                                        Push(client, ClientRequests.MetaClientPlayerListPanel_0x02_0x00_0x00, otherCharacter.Id); // 14_8
                                }
                            }

                            /*
                                foreach (KeyValuePair<int, Character> q in _characters)
                                {
                                    Character otherCharacter = q.Value;

                                    if ((otherCharacter.State & Character.States.IsHumanOnline) == Character.States.IsOnline)
                                    {
                                        if (idList.TryAdd(otherCharacter.Id, null))
                                            Push(client, ClientRequests.MetaClientPlayerListPanel_0x02_0x00_0x00, otherCharacter.Id); // 14_8
                                    }
                                }
                            */

                            Contract.Assert(queueInt.Count == 0);

                            foreach (KeyValuePair<int, object> q in idList)
                            {
                                int otherCharacterId = q.Key;

                                if (!_characters.ContainsKey(otherCharacterId))
                                    queueInt.Enqueue(otherCharacterId);
                            }

                            while (queueInt.TryDequeue(out int otherCharacterId))
                            {
                                idList.Remove(otherCharacterId);

                                Push(client, ClientRequests.MetaClientPlayerListPanel_0x03_0x00_0x01, otherCharacterId); // nothing
                            }

                            TryWrite(client);
                        }

                        TryWriteIconRequests(client);
                        TryWriteHexRequests(client);
                    }
                }
            }
        }
    }
}
