using System;
using System.Buffers;
using System.Diagnostics.Contracts;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace shrNet
{
    public static class DuplexSocket
    {
        public static void Initialize(Socket socket, int receiveTimeout, int sendTimout)
        {
            Initialize(socket);

            socket.ReceiveTimeout = receiveTimeout;
            socket.SendTimeout = sendTimout;
        }

        public static void Initialize(Socket socket)
        {
            Contract.Assert(
                socket.Blocking &&
                socket.DontFragment &&
                socket.ReceiveBufferSize == 65536 &&
                socket.SendBufferSize == 65536
            );

            socket.NoDelay = true;
        }

#if VERBOSE
        private static readonly byte[] _hexadecimals =
        [
            (byte)'0', (byte)'1', (byte)'2', (byte)'3',
            (byte)'4', (byte)'5', (byte)'6', (byte)'7',
            (byte)'8', (byte)'9', (byte)'a', (byte)'b',
            (byte)'c', (byte)'d', (byte)'e', (byte)'f'
        ];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AppendHex(StringBuilder log, ReadOnlySpan<byte> bytes)
        {
            nint length = bytes.Length;

            if (length == 0)
                return;

            char[] chars = ArrayPool<char>.Shared.Rent((int)(length << 1));

            ref byte source = ref MemoryMarshal.GetReference(bytes);
            ref byte hexadecimals = ref MemoryMarshal.GetArrayDataReference(_hexadecimals);
            ref uint destiny = ref Unsafe.As<char, uint>(ref MemoryMarshal.GetArrayDataReference(chars));

            for (nint i = 0; i < length; i++)
            {
                nint a = Unsafe.Add(ref source, i);

                uint b = Unsafe.Add(ref hexadecimals, a & 15);
                uint c = Unsafe.Add(ref hexadecimals, a >> 4);

                Unsafe.WriteUnaligned(ref Unsafe.As<uint, byte>(ref Unsafe.Add(ref destiny, i)), b << 16 | c);
            }

            log.Append(chars, 0, (int)(length << 1));

            ArrayPool<char>.Shared.Return(chars);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AppendUtf8(StringBuilder log, ReadOnlySpan<byte> bytes)
        {
            log.Append(Encoding.UTF8.GetString(bytes));
        }
#endif

    }
}
