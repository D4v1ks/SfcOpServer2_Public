#pragma warning disable IDE1006

using System.Diagnostics.Contracts;
using System.IO;

namespace shrQ3
{
    public sealed class tRadioGroupAsset : tVisualGroupAsset
    {
        public tString Target;

        public override int Length => base.Length + Target.Length;

        public tRadioGroupAsset(int id, string name) : base(id, name, eType.RadioGroup)
        { }

        public tRadioGroupAsset(BinaryReader r) : base(r)
        { }

        public override void ReadFrom(BinaryReader r)
        {
            base.ReadFrom(r);

            Target = new(r);

            Contract.Assert(Target.Value.Length == 0);
        }

        public override void WriteTo(BinaryWriter w)
        {
            base.WriteTo(w);

            Target.WriteTo(w);
        }
    }
}
