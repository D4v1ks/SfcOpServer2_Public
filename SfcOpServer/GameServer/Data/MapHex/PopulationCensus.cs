#pragma warning disable IDE0130

using System.IO;

namespace SfcOpServer
{
    public sealed class PopulationCensus
    {
        public int[] RaceCount;
        public int[] RaceBPV;

        public int[] AllyCount;
        public int[] AllyBPV;

        public int[] EnemyCount;
        public int[] EnemyBPV;

        public int[] NeutralCount;
        public int[] NeutralBPV;

        public PopulationCensus()
        {
            RaceCount = new int[(int)Races.kNumberOfRaces];
            RaceBPV = new int[(int)Races.kNumberOfRaces];

            AllyCount = new int[(int)Races.kNumberOfRaces];
            AllyBPV = new int[(int)Races.kNumberOfRaces];

            EnemyCount = new int[(int)Races.kNumberOfRaces];
            EnemyBPV = new int[(int)Races.kNumberOfRaces];

            NeutralCount = new int[(int)Races.kNumberOfRaces];
            NeutralBPV = new int[(int)Races.kNumberOfRaces];
        }

        public void ReadFrom(BinaryReader r)
        {
            for (int i = 0; i < (int)Races.kNumberOfRaces; i++)
            {
                RaceCount[i] = r.ReadInt32();
                RaceBPV[i] = r.ReadInt32();

                AllyCount[i] = r.ReadInt32();
                AllyBPV[i] = r.ReadInt32();

                EnemyCount[i] = r.ReadInt32();
                EnemyBPV[i] = r.ReadInt32();

                NeutralCount[i] = r.ReadInt32();
                NeutralBPV[i] = r.ReadInt32();
            }
        }

        public void WriteTo(BinaryWriter w)
        {
            for (int i = 0; i < (int)Races.kNumberOfRaces; i++)
            {
                w.Write(RaceCount[i]);
                w.Write(RaceBPV[i]);

                w.Write(AllyCount[i]);
                w.Write(AllyBPV[i]);

                w.Write(EnemyCount[i]);
                w.Write(EnemyBPV[i]);

                w.Write(NeutralCount[i]);
                w.Write(NeutralBPV[i]);
            }
        }
    }
}
