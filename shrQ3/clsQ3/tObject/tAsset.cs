#pragma warning disable IDE1006

using System;
using System.Diagnostics.Contracts;
using System.IO;

namespace shrQ3
{
    public abstract class tAsset : tObject
    {
        public enum eType : short
        {
            Bmp = 1,
            Scene = 3,
            Button = 8,
            TextEdit = 9,
            StateBtn = 10,
            Slider = 11,
            ScrollBox = 15,
            RadioGroup = 17,
            TextRect = 21,
            GenericRect = 25
        }

        public eType Type;
        public int Id;
        public short Version;
        public tString Name;
        public tString SubType;

        public override int Length => Name.Length + SubType.Length + 8;

        public tAsset()
        { }

        public tAsset(eType type, int id, string name)
        {
            Type = type;
            Id = id;
            Version = 153;
            Name = new(name);
            SubType = new();
        }

        public tAsset(BinaryReader r)
        {
            ReadFrom(r);
        }

        public override void ReadFrom(BinaryReader r)
        {
            Type = (eType)r.ReadInt16();
            Id = r.ReadInt32();
            Version = r.ReadInt16();
            Name = new(r);
            SubType = new(r);

            Contract.Assert(Enum.IsDefined(Type) && Id >= 1 && Version == 153);
        }

        public override void WriteTo(BinaryWriter w)
        {
            w.Write((short)Type);
            w.Write(Id);
            w.Write(Version);
            Name.WriteTo(w);
            SubType.WriteTo(w);
        }
    }
}
