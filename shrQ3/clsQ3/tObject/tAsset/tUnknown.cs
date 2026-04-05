#pragma warning disable IDE1006

using System;
using System.Buffers;
using System.Diagnostics.Contracts;
using System.IO;

namespace shrQ3
{
    public sealed class tUnknown(BinaryReader r) : tAsset(r)
    {
        public byte[] data;

        public override int Length => base.Length + data.Length;

        public override void ReadFrom(BinaryReader r)
        {
            base.ReadFrom(r);

            ReadUnknownBytesFrom(r.BaseStream);

            Contract.Assert(data.Length != 0);
        }

        public override void WriteTo(BinaryWriter w)
        {
            base.WriteTo(w);

            Contract.Assert(data.Length != 0);

            w.Write(data);
        }

        private unsafe void ReadUnknownBytesFrom(Stream s)
        {
            long offset = s.Position;

            Contract.Assert(offset < s.Length);

            const int bufferLength = 2048;

            byte[] b = ArrayPool<byte>.Shared.Rent(bufferLength);
            int c = s.Read(b, 0, bufferLength);

            int nextId = Id + 1;

            fixed (byte* b0 = b)
            {
                byte* b1 = b0;
                byte* b2 = b0 + c - 8;

                while (b1 < b2)
                {
                    short type = *(short*)b1;
                    int id = *(int*)(b1 + 2);
                    ushort version = *(ushort*)(b1 + 6);

                    if (Enum.IsDefined(typeof(eType), type) && id == nextId && version == 153)
                    {
                        c = (int)(b1 - b0);

                        break;
                    }

                    b1++;
                }
            }

            Contract.Assert(c > 0);

            data = new byte[c];

            Buffer.BlockCopy(b, 0, data, 0, c);

            ArrayPool<byte>.Shared.Return(b);

            offset += c;

            if (offset < s.Length)
                s.Seek(offset, SeekOrigin.Begin);
        }
    }
}
