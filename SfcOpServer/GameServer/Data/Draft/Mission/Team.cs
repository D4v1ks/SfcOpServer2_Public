#pragma warning disable IDE0130

using System.Collections.Generic;
using System.IO;

namespace SfcOpServer
{
    public sealed class Team
    {
        public int OwnerId;

        public TeamIds Id;
        public TeamTypes Type;
        public TeamTags Tag;

        public Dictionary<int, object> Ships; // ship id, null
        public Dictionary<int, int> Reported; // ship id, ship damage

        public Team(Character teamOwner, TeamIds teamId, TeamTypes teamType, TeamTags teamTag)
        {
            OwnerId = teamOwner.Id;

            Id = teamId;
            Type = teamType;
            Tag = teamTag;

            Ships = [];
            Reported = [];
        }

        /// <summary>
        /// This function is for debugging
        /// </summary>
        /// <param name="r"></param>
        public Team(BinaryReader r)
        {
            OwnerId = r.ReadInt32();

            Id = (TeamIds)r.ReadInt32();
            Type = (TeamTypes)r.ReadInt32();
            Tag = (TeamTags)r.ReadInt32();

            Ships = [];

            int c = r.ReadInt32();

            for (int i = 0; i < c; i++)
                Ships.Add(r.ReadInt32(), null);

            c = r.ReadInt32();

            for (int i = 0; i < c; i++)
                Reported.Add(r.ReadInt32(), r.ReadInt32());
        }

        /// <summary>
        /// This function is for debugging
        /// </summary>
        /// <param name="w"></param>
        public void WriteTo(BinaryWriter w)
        {
            w.Write(OwnerId);

            w.Write((int)Id);
            w.Write((int)Type);
            w.Write((int)Tag);

            w.Write(Ships.Count);

            foreach (KeyValuePair<int, object> p in Ships)
                w.Write(p.Key);

            w.Write(Reported.Count);

            foreach (KeyValuePair<int, int> p in Reported)
            {
                w.Write(p.Key);
                w.Write(p.Value);
            }
        }
    }
}
