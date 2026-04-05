#pragma warning disable IDE1006

using System.Diagnostics.Contracts;
using System.IO;

namespace shrQ3
{
    public sealed class tGenericRectAsset : tBoundAsset
    {
        public tString Extra;

        public override int Length => base.Length + Extra.Length;

        public tGenericRectAsset(int id, string name) : base(eType.GenericRect, id, name)
        {
            Extra = new();
        }

        public tGenericRectAsset(BinaryReader r) : base(r)
        { }

        public override void ReadFrom(BinaryReader r)
        {
            base.ReadFrom(r);

            Extra = new(r);

            Contract.Assert(Extra.Value.Length == 0);
        }

        public override void WriteTo(BinaryWriter w)
        {
            base.WriteTo(w);

            Extra.WriteTo(w);
        }
    }
}
