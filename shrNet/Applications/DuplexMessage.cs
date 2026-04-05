using System;
using System.Buffers;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

namespace shrNet
{
    public readonly struct DuplexMessage
    {
        public readonly int Id;

        public readonly byte[] Buffer;
        public readonly int Length;

        public DuplexMessage(int id, int length)
        {
            Id = id;

            Buffer = ArrayPool<byte>.Shared.Rent(length);
            Length = length;
        }

        public DuplexMessage(int id, byte[] buffer, int length)
        {
            Contract.Assert(buffer != null);

            Id = id;

            Buffer = buffer;
            Length = length;
        }

        public void Release()
        {
            ArrayPool<byte>.Shared.Return(Buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<byte> AsSpan()
        {
            return new(Buffer, 0, Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<byte> AsReadOnlySpan()
        {
            return new(Buffer, 0, Length);
        }
    }
}
