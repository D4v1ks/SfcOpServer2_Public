#pragma warning disable IDE0130

using System.Collections.Generic;
using System.IO;

namespace SfcOpServer
{
    public sealed class MapHex : IObject
    {
        public readonly static MapHex Empty;
        public readonly static MapHex FogOfWar;

        // data

        public int Id { get; set; }

        public int X;
        public int Y;

        public Races EmpireControl;
        public Races CartelControl;

        public int Terrain;
        public int Planet;
        public int Base;

        public TerrainTypes TerrainType;
        public PlanetTypes PlanetType;
        public BaseTypes BaseType;

        public int BaseEconomicPoints;
        public int CurrentEconomicPoints;

        public int EmpireBaseVictoryPoints;
        public int EmpireCurrentVictoryPoints;

        public int CartelBaseVictoryPoints;
        public int CartelCurrentVictoryPoints;

        public double BaseSpeedPoints;
        public double CurrentSpeedPoints;

        // helpers

        public double[] ControlPoints;

        public bool IsEmpireHome;
        public bool IsCartelHome;

        public long Mission;

        public Dictionary<int, object> Population;
        public PopulationCensus Census;

        // constructors

        static MapHex()
        {
            Empty = new(false);
            FogOfWar = new(false)
            {
                TerrainType = TerrainTypes.kTerrainAsteroids3,

                EmpireControl = Races.kJindarian,
                CartelControl = Races.kJindarian
            };
        }

        public MapHex(bool initialize)
        {
            if (initialize)
                Initialize();
        }

        public MapHex(BinaryReader r)
        {
            Initialize();

            // data

            Id = r.ReadInt32();

            X = r.ReadInt32();
            Y = r.ReadInt32();

            EmpireControl = (Races)r.ReadInt32();
            CartelControl = (Races)r.ReadInt32();

            Terrain = r.ReadInt32();
            Planet = r.ReadInt32();
            Base = r.ReadInt32();

            TerrainType = (TerrainTypes)r.ReadInt32();
            PlanetType = (PlanetTypes)r.ReadInt32();
            BaseType = (BaseTypes)r.ReadInt32();

            BaseEconomicPoints = r.ReadInt32();
            CurrentEconomicPoints = r.ReadInt32();

            EmpireBaseVictoryPoints = r.ReadInt32();
            EmpireCurrentVictoryPoints = r.ReadInt32();

            CartelBaseVictoryPoints = r.ReadInt32();
            CartelCurrentVictoryPoints = r.ReadInt32();

            BaseSpeedPoints = r.ReadDouble();
            CurrentSpeedPoints = r.ReadDouble();

            // helpers

            for (int i = 0; i < (int)Races.kNumberOfRaces; i++)
                ControlPoints[i] = r.ReadDouble();

            IsEmpireHome = r.ReadBoolean();
            IsCartelHome = r.ReadBoolean();

            Mission = r.ReadInt64();

            while (true)
            {
                int characterId = r.ReadInt32();

                if (characterId == 0)
                    break;

                Population.Add(characterId, null);
            }

            Census.ReadFrom(r);
        }

        public void WriteTo(BinaryWriter w)
        {
            // data

            w.Write(Id);

            w.Write(X);
            w.Write(Y);

            w.Write((int)EmpireControl);
            w.Write((int)CartelControl);

            w.Write(Terrain);
            w.Write(Planet);
            w.Write(Base);

            w.Write((int)TerrainType);
            w.Write((int)PlanetType);
            w.Write((int)BaseType);

            w.Write(BaseEconomicPoints);
            w.Write(CurrentEconomicPoints);

            w.Write(EmpireBaseVictoryPoints);
            w.Write(EmpireCurrentVictoryPoints);

            w.Write(CartelBaseVictoryPoints);
            w.Write(CartelCurrentVictoryPoints);

            w.Write(BaseSpeedPoints);
            w.Write(CurrentSpeedPoints);

            // helpers

            for (int i = 0; i < (int)Races.kNumberOfRaces; i++)
                w.Write(ControlPoints[i]);

            w.Write(IsEmpireHome);
            w.Write(IsCartelHome);

            w.Write(Mission);

            foreach (KeyValuePair<int, object> p in Population)
                w.Write(p.Key);

            w.Write(0);

            Census.WriteTo(w);
        }

        // private functions

        private void Initialize()
        {
            ControlPoints = new double[(int)Races.kNumberOfRaces];

            Population = new(64);
            Census = new PopulationCensus();
        }
    }
}
