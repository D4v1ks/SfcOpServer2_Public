#pragma warning disable CA2211, IDE1006

using System.IO;

namespace shrQ3
{
    public abstract class tObject
    {

#if DEBUG
        public static clsQ3 Parent;
#endif

        public abstract int Length { get; }

        public abstract void ReadFrom(BinaryReader r);
        public abstract void WriteTo(BinaryWriter w);
    }
}
