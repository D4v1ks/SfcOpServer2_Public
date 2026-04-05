#pragma warning disable IDE0130

using System.IO;

namespace SfcOpServer
{
    public struct WeaponHardpoint
    {
        public WeaponStates State;
        public WeaponArcs Arc;

        public void ReadFrom(BinaryReader r)
        {
            State = (WeaponStates)r.ReadInt16();
            Arc = (WeaponArcs)r.ReadInt16();
        }

        public readonly void WriteTo(BinaryWriter w)
        {
            w.Write((short)State);
            w.Write((short)Arc);
        }
    }
}
