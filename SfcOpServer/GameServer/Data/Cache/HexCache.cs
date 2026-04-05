#pragma warning disable IDE0130

using System.IO;

namespace SfcOpServer
{
    internal struct HexCache
    {
        public int Id;
        public int LockId;
        public int X;
        public int Y;
        public int ControlCount;
        public ControlCache[] Controls;
        public TerrainTypes TerrainType;
        public PlanetTypes PlanetType;
        public BaseTypes BaseType;
        public int BaseEconomicPoints;
        public int CurrentEconomicPoints;
        public int BaseVictoryCount;
        public VictoryCache[] BaseVictories;
        public int CurrentVictoryCount;
        public VictoryCache[] CurrentVictories;
        public double BaseSpeedPoints;
        public double CurrentSpeedPoints;

        public HexCache(BinaryReader r)
        {
            Id = r.ReadInt32();
            LockId = r.ReadInt32();
            X = r.ReadInt32();
            Y = r.ReadInt32();
            ControlCount = r.ReadInt32();

            if (ControlCount == 0)
                Controls = [];
            else
            {
                Controls = new ControlCache[ControlCount];

                for (int i = 0; i < ControlCount; i++)
                    Controls[i] = new ControlCache(r);
            }

            TerrainType = (TerrainTypes)r.ReadInt32();
            PlanetType = (PlanetTypes)r.ReadInt32();
            BaseType = (BaseTypes)r.ReadInt32();
            BaseEconomicPoints = r.ReadInt32();
            CurrentEconomicPoints = r.ReadInt32();
            BaseVictoryCount = r.ReadInt32();

            if (BaseVictoryCount == 0)
                BaseVictories = [];
            else
            {
                BaseVictories = new VictoryCache[BaseVictoryCount];

                for (int i = 0; i < BaseVictoryCount; i++)
                    BaseVictories[i] = new VictoryCache(r);
            }

            CurrentVictoryCount = r.ReadInt32();

            if (CurrentVictoryCount == 0)
                CurrentVictories = [];
            else
            {
                CurrentVictories = new VictoryCache[CurrentVictoryCount];

                for (int i = 0; i < CurrentVictoryCount; i++)
                    CurrentVictories[i] = new VictoryCache(r);
            }

            BaseSpeedPoints = r.ReadDouble();
            CurrentSpeedPoints = r.ReadDouble();
        }
    }
}
