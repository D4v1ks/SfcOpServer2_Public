#pragma warning disable IDE0130

using System;
using System.Collections.Generic;
using System.IO;

namespace SfcOpServer
{
    public sealed class Draft
    {
        public int Countdown;
        public DateTime TimeStamp;

        public Dictionary<int, object> Expected;
        public Dictionary<int, object> Accepted;
        public Dictionary<int, object> Forfeited;

        public Dictionary<int, object> Confirmed;
        public Dictionary<int, object> Ready;

        public Dictionary<int, object> Reported;

        public Mission Mission;

        public Draft()
        {
            Expected = [];
            Accepted = [];
            Forfeited = [];

            Confirmed = [];
            Ready = [];

            Reported = [];
        }

        /// <summary>
        /// This function is for debugging
        /// </summary>
        /// <param name="r"></param>
        public Draft(BinaryReader r)
        {
            Countdown = r.ReadInt32();
            TimeStamp = DateTime.FromBinary(r.ReadInt64());

            //-------------------------------------------

            Expected = [];

            int c = r.ReadInt32();

            for (int i = 0; i < c; i++)
                Expected.Add(r.ReadInt32(), null);

            //-------------------------------------------

            Accepted = [];

            c = r.ReadInt32();

            for (int i = 0; i < c; i++)
                Accepted.Add(r.ReadInt32(), null);

            //-------------------------------------------

            Forfeited = [];

            c = r.ReadInt32();

            for (int i = 0; i < c; i++)
                Forfeited.Add(r.ReadInt32(), null);

            //-------------------------------------------

            Confirmed = [];

            c = r.ReadInt32();

            for (int i = 0; i < c; i++)
                Confirmed.Add(r.ReadInt32(), null);

            //-------------------------------------------

            Ready = [];

            c = r.ReadInt32();

            for (int i = 0; i < c; i++)
                Ready.Add(r.ReadInt32(), null);

            //-------------------------------------------

            Reported = [];

            c = r.ReadInt32();

            for (int i = 0; i < c; i++)
                Reported.Add(r.ReadInt32(), null);

            //-------------------------------------------

            Mission = new Mission(r);
        }

        /// <summary>
        /// This function is for debugging
        /// </summary>
        /// <param name="w"></param>
        public void WriteTo(BinaryWriter w)
        {
            w.Write(Countdown);
            w.Write(TimeStamp.ToBinary());

            //-------------------------------------------------------

            w.Write(Expected.Count);

            foreach (KeyValuePair<int, object> p in Expected)
                w.Write(p.Key);

            //-------------------------------------------------------

            w.Write(Accepted.Count);

            foreach (KeyValuePair<int, object> p in Accepted)
                w.Write(p.Key);

            //-------------------------------------------------------

            w.Write(Forfeited.Count);

            foreach (KeyValuePair<int, object> p in Forfeited)
                w.Write(p.Key);

            //-------------------------------------------------------

            w.Write(Confirmed.Count);

            foreach (KeyValuePair<int, object> p in Confirmed)
                w.Write(p.Key);

            //-------------------------------------------------------

            w.Write(Ready.Count);

            foreach (KeyValuePair<int, object> p in Ready)
                w.Write(p.Key);

            //-------------------------------------------------------

            w.Write(Reported.Count);

            foreach (KeyValuePair<int, object> p in Reported)
                w.Write(p.Key);

            //-------------------------------------------------------

            Mission.WriteTo(w);
        }
    }
}
