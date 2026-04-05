#pragma warning disable IDE0130

using System.Collections.Generic;
using System.IO;

namespace SfcOpServer
{
    public sealed class Mission
    {
        // constants

        public const int FirstCustomId = 100;

        // variables

        public int HostId;

        public int Map;
        public string Background;
        public int Speed;

        public Dictionary<int, Team> Teams; // character id, team

        public Dictionary<int, int> CustomIds; // custom id, ship id 
        public string Config;
        public string[] Musics;

        public Mission()
        { }

        /// <summary>
        /// This function is for debugging
        /// </summary>
        /// <param name="r"></param>
        public Mission(BinaryReader r)
        {
            HostId = r.ReadInt32();

            Map = r.ReadInt32();
            Background = r.ReadString();
            Speed = r.ReadInt32();

            Teams = [];

            int c = r.ReadInt32();

            for (int i = 0; i < c; i++)
                Teams.Add(r.ReadInt32(), new Team(r));

            CustomIds = [];

            c = r.ReadInt32();

            for (int i = 0; i < c; i++)
                CustomIds.Add(r.ReadInt32(), r.ReadInt32());

            Config = r.ReadString();
        }

        /// <summary>
        /// This function is for debugging
        /// </summary>
        /// <param name="w"></param>
        public void WriteTo(BinaryWriter w)
        {
            w.Write(HostId);

            w.Write(Map);
            w.Write(Background);
            w.Write(Speed);

            w.Write(Teams.Count);

            foreach (KeyValuePair<int, Team> p in Teams)
            {
                w.Write(p.Key);
                p.Value.WriteTo(w);
            }

            foreach (KeyValuePair<int, int> p in CustomIds)
            {
                w.Write(p.Key);
                w.Write(p.Value);
            }

            w.Write(Config);
        }
    }
}
