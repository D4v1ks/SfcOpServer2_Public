#pragma warning disable IDE1006

using System;
using System.IO;
using System.Text;

namespace shrQ3
{
    public sealed class tString : tObject
    {
        public string Value;

        public override int Length => Value.Length + 4;

        public tString()
        {
            Value = string.Empty;
        }

        public tString(string value)
        {
            Value = value;
        }

        public tString(BinaryReader r)
        {
            ReadFrom(r);
        }

        public override void ReadFrom(BinaryReader r)
        {
            int c = r.ReadInt32();

            if (c == 0)
            {
                Value = string.Empty;

                return;
            }

            if (c > 0)
            {
                Span<byte> b = stackalloc byte[c];

                if (r.Read(b) == c)
                {
                    Value = Encoding.UTF8.GetString(b);

                    return;
                }
            }

            throw new NotSupportedException();
        }

        public override void WriteTo(BinaryWriter w)
        {
            int c = Value.Length;

            w.Write(c);

            if (c != 0)
            {
                Span<byte> b = stackalloc byte[c];

                if (Encoding.UTF8.GetBytes(Value.AsSpan(), b) != c)
                    throw new NotSupportedException();

                w.Write(b);
            }
        }
    }
}
