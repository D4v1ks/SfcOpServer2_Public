#pragma warning disable IDE0130

using System.IO;

namespace SfcOpServer
{
    public struct StoreItem
    {
        public byte MaxQuantity;
        public byte BaseQuantity;
        public byte CurrentQuantity;

        public void ReadFrom(BinaryReader r)
        {
            MaxQuantity = r.ReadByte();
            BaseQuantity = r.ReadByte();
            CurrentQuantity = r.ReadByte();
        }

        public readonly void WriteTo(BinaryWriter w)
        {
            w.Write(MaxQuantity);
            w.Write(BaseQuantity);
            w.Write(CurrentQuantity);
        }
    }
}
