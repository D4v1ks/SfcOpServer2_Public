#pragma warning disable IDE1006

using System.Diagnostics.Contracts;
using System.IO;

namespace shrQ3
{
    public sealed class tCodeAsset : tObject
    {
        public tUserHandler[] Users;
        public int Unused1;
        public byte[] Opcodes;
        public int Unused2;

        public override int Length
        {
            get
            {
                int r = Opcodes.Length + 16;

                for (int i = 0; i < Users.Length; i++)
                    r += Users[i].Length;

                return r;
            }
        }

        public tCodeAsset()
        {
            Users = [];
            Opcodes = [];
        }

        public tCodeAsset(BinaryReader r)
        {
            ReadFrom(r);
        }

        public override void ReadFrom(BinaryReader r)
        {
            int c = r.ReadInt32();

            if (c > 0)
            {
                Users = new tUserHandler[c];

                for (int i = 0; i < c; i++)
                    Users[i] = new(r);
            }
            else
            {
                Contract.Assert(c == 0);

                Users = [];
            }

            Unused1 = r.ReadInt32();

            c = r.ReadInt32();

            if (c > 0)
                Opcodes = r.ReadBytes(c);
            else
            {
                Contract.Assert(c == 0);

                Opcodes = [];
            }

            Unused2 = r.ReadInt32();

            Contract.Assert(Unused1 == 0 && Unused2 == 0);
        }

        public override void WriteTo(BinaryWriter w)
        {
            int c = Users.Length;

            w.Write(c);

            for (int i = 0; i < c; i++)
                Users[i].WriteTo(w);

            w.Write(Unused1);

            c = Opcodes.Length;

            w.Write(c);

            if (c > 0)
                w.Write(Opcodes);

            w.Write(Unused2);
        }
    }
}