#pragma warning disable IDE1006

using System.IO;

namespace shrQ3
{
    public sealed class tUserHandler : tObject
    {
        public tString Name;
        public uint Index;

        public override int Length => Name.Length + 4;

        public tUserHandler()
        {
            Name = new();
        }

        public tUserHandler(BinaryReader r)
        {
            ReadFrom(r);
        }

        public override void ReadFrom(BinaryReader r)
        {
            Name = new(r);
            Index = r.ReadUInt32();
        }

        public override void WriteTo(BinaryWriter w)
        {
            Name.WriteTo(w);
            w.Write(Index);
        }
    }
}
