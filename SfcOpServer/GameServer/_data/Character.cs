#pragma warning disable IDE0130

using shrServices;

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Runtime.CompilerServices;

namespace SfcOpServer
{
    public sealed class Character : IObject
    {
        public readonly static Character Empty = new(false);

        private sealed class Comparer : IComparer<IObject>
        {
            public int Compare(IObject object1, IObject object2)
            {
                Contract.Assert(object1 is Ship && object2 is Ship);

                Ship a = (Ship)object1;
                Ship b = (Ship)object2;

                int p = 0;

                if (a.ClassType != b.ClassType)
                {
                    if (a.ClassType < b.ClassType)
                        p += 10;
                    else
                        p -= 10;

                    if (((1 << (int)a.ClassType) & GameServer.ClassTypeIconMask) == 0) p += 1000;
                    if (((1 << (int)b.ClassType) & GameServer.ClassTypeIconMask) == 0) p -= 1000;
                }

                if (a.BPV < b.BPV)
                    p += 100;
                else if (a.BPV > b.BPV)
                    p -= 100;

                return Math.Sign(p);
            }
        }

        private readonly static Comparer _comparer = new();

        // enumerations

        public enum States
        {
            None = 0,

            // basic states

            IsCpu = 1 << 0,
            IsHuman = 1 << 1,

            IsAfk = 1 << 2,
            IsBusy = 1 << 3,
            IsOnline = 1 << 4,

            IsConnecting = 1 << 5,
            IsReconnecting = 1 << 6,

            IsDisconnecting = 1 << 7,

            // cpu valid states

            IsCpuOnline = IsCpu | IsOnline,
            IsCpuBusyOnline = IsCpu | IsBusy | IsOnline,
            IsCpuAfkBusyOnline = IsCpu | IsAfk | IsBusy | IsOnline,

            // human valid states

            IsHumanOnline = IsHuman | IsOnline,
            IsHumanAfkOnline = IsHuman | IsAfk | IsOnline,
            IsHumanBusyOnline = IsHuman | IsBusy | IsOnline,
            IsHumanAfkBusyOnline = IsHuman | IsAfk | IsBusy | IsOnline,

            IsHumanBusyConnecting = IsHuman | IsBusy | IsConnecting,
            IsHumanBusyReconnecting = IsHuman | IsBusy | IsReconnecting,

            IsHumanBusyDisconnecting = IsHuman | IsBusy | IsDisconnecting
        }

        // data

        public string IPAddress;
        public string WONLogon;

        public int Id { get; set; }

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

        public int ShipCount => _shipCount;

        private int _shipCount;
        private readonly IObject[] _shipList;

        // helpers

        public int ShipListBPV;

        public int BestShipId;
        public ClassTypes BestShipClass;
        public int BestShipBPV;

        public Medals Awards;
        public int Bids;
        public long Mission;

        public States State;

        public DateTime LastLogin;
        public DateTime LastLogout;

        // references

        public Client27000 Client;

        // constructors

        public Character(bool initialize)
        {
            // data

            IPAddress = string.Empty;
            WONLogon = string.Empty;

            CharacterName = string.Empty;

            CharacterRank = Ranks.None;
            CharacterRating = 1500;

            CharacterLocationX = -1;
            CharacterLocationY = -1;
            HomeWorldLocationX = -1;
            HomeWorldLocationY = -1;
            MoveDestinationX = -1;
            MoveDestinationY = -1;

            if (initialize)
                _shipList = new IObject[GameServer.MaxFleetSize];

            // helpers

            Awards = Medals.kMedalRankOne;
        }

        public Character(BinaryReader r)
        {
            // data

            Utils.Read(r, false, out IPAddress);
            Utils.Read(r, false, out WONLogon);

            Id = r.ReadInt32();

            Utils.Read(r, false, out CharacterName);

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

            _shipCount = r.ReadInt32();
            _shipList = new IObject[GameServer.MaxFleetSize];

            for (int i = 0; i < _shipCount; i++)
                _shipList[i] = new PlaceHolder(r.ReadInt32());

            // helpers

            ShipListBPV = r.ReadInt32();

            BestShipId = r.ReadInt32();
            BestShipClass = (ClassTypes)r.ReadInt32();
            BestShipBPV = r.ReadInt32();

            Awards = (Medals)r.ReadInt32();
            Bids = r.ReadInt32();
            Mission = r.ReadInt64();

            State = (States)r.ReadInt64();

            LastLogin = DateTime.FromBinary(r.ReadInt64());
            LastLogout = DateTime.FromBinary(r.ReadInt64());
        }

        public void WriteTo(BinaryWriter w)
        {
            // data

            Utils.Write(w, false, IPAddress);
            Utils.Write(w, false, WONLogon);

            w.Write(Id);

            Utils.Write(w, false, CharacterName);

            w.Write((int)CharacterRace);
            w.Write((int)CharacterPoliticalControl);
            w.Write((int)CharacterRank);
            w.Write(CharacterRating);
            w.Write(CharacterCurrentPrestige);
            w.Write(CharacterLifetimePrestige);
            w.Write(Unknown);
            w.Write(CharacterLocationX);
            w.Write(CharacterLocationY);
            w.Write(HomeWorldLocationX);
            w.Write(HomeWorldLocationY);
            w.Write(MoveDestinationX);
            w.Write(MoveDestinationY);

            w.Write(_shipCount);

            for (int i = 0; i < _shipCount; i++)
                w.Write(_shipList[i].Id);

            // helpers

            w.Write(ShipListBPV);

            w.Write(BestShipId);
            w.Write((int)BestShipClass);
            w.Write(BestShipBPV);

            w.Write((int)Awards);
            w.Write(Bids);
            w.Write(Mission);

            w.Write((long)State);

            w.Write(LastLogin.ToBinary());
            w.Write(LastLogout.ToBinary());
        }

        public void Update(Dictionary<int, Ship> ships)
        {
            for (int i = 0; i < _shipCount; i++)
            {
                if (_shipList[i] != null && _shipList[i] is PlaceHolder)
                    _shipList[i] = ships[_shipList[i].Id];
            }
        }

        // public functions

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void ClearShipList()
        {
            // data

            for (int i = 0; i < _shipCount; i++)
            {
                Contract.Assert(_shipList[i] != null);

                _shipList[i] = null;
            }

            _shipCount = 0;

            // helpers

            ShipListBPV = 0;

            BestShipId = 0;
            BestShipClass = ClassTypes.kClassShuttle;
            BestShipBPV = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void AddShip(Ship ship)
        {
            Contract.Assert(ship != null);

            // data

            _shipList[_shipCount] = ship;

            _shipCount++;

            if (_shipCount > 1)
                Array.Sort(_shipList, 0, _shipCount, _comparer);

            // helpers

            ShipListBPV += ship.BPV;

            PromoteFirstShip();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Ship GetFirstShip()
        {
            Contract.Assert(_shipList[0] != null && _shipList[0] is Ship);

            return (Ship)_shipList[0];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Ship GetShipAt(int index)
        {
            Contract.Assert(index >= 0 && index < _shipCount && _shipList[index] != null && _shipList[index] is Ship);

            return (Ship)_shipList[index];
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public int TryGetShip(int shipId, out Ship ship)
        {
            for (int i = 0; i < _shipCount; i++)
            {
                Contract.Assert(_shipList[i] != null && _shipList[i] is Ship);

                ship = (Ship)_shipList[i];

                if (ship.Id == shipId)
                    return i;
            }

            ship = null;

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public int TryRemoveShip(int shipId)
        {
            if (_shipCount == 1)
            {
                Contract.Assert(_shipList[0] != null);

                if (_shipList[0].Id == shipId)
                {
                    ClearShipList();

                    return 0;
                }
            }
            else if (_shipCount >= 2)
            {
                for (int i = 0; i < _shipCount; i++)
                {
                    Contract.Assert(_shipList[i] != null);

                    if (_shipList[i].Id == shipId)
                    {
                        RemoveShipAt(i);

                        return i;
                    }
                }
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void RemoveShipAt(int index)
        {
            Contract.Assert(index >= 0 && index < _shipCount && _shipList[index] != null && _shipList[index] is Ship);

            Ship ship = (Ship)_shipList[index];

            // data

            _shipCount--;

            while (index < _shipCount)
            {
                _shipList[index] = _shipList[index + 1];

                index++;
            }

            _shipList[index] = null;

            // helpers

            ShipListBPV -= ship.BPV;

            if (index == 0)
                PromoteFirstShip();
        }

        // private ShipList functions

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PromoteFirstShip()
        {
            Ship ship = (Ship)_shipList[0];

            BestShipId = ship.Id;
            BestShipClass = ship.ClassType;
            BestShipBPV = ship.BPV;
        }
    }
}
