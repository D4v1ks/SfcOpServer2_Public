using shrNet;

using System;
using System.Buffers;
using System.Net;
using System.Threading.Tasks;

namespace shrServices
{
    public sealed class DuplexService15101 : DuplexService
    {
        private static byte[][] _data;

        public static void Initialize(IPAddress publicIP)
        {
            byte[] firewallDetectorPort = BitConverter.GetBytes((ushort)15300);
            byte[] firewallDetectorAddress = publicIP.GetAddressBytes();

            byte[] directoryServerTime = BitConverter.GetBytes(TimeService.NewsTime());

            _data =
            [
                [63, 0, 0, 0, 5, 2, 0, 103, 0, 55, 8, 2, 6, 0, 0, 22, 0, 47, 0, 84, 0, 105, 0, 116, 0, 97, 0, 110, 0, 83, 0, 101, 0, 114, 0, 118, 0, 101, 0, 114, 0, 115, 0, 47, 0, 70, 0, 105, 0, 114, 0, 101, 0, 119, 0, 97, 0, 108, 0, 108, 0, 0, 0],
                [97, 0, 0, 0, 5, 2, 0, 3, 0, 0, 0, 128, 55, 8, 2, 6, 2, 0, 68, 8, 0, 70, 0, 105, 0, 114, 0, 101, 0, 119, 0, 97, 0, 108, 0, 108, 0, 0, 0, 0, 0, 83, 21, 0, 84, 0, 105, 0, 116, 0, 97, 0, 110, 0, 70, 0, 105, 0, 114, 0, 101, 0, 119, 0, 97, 0, 108, 0, 108, 0, 68, 0, 101, 0, 116, 0, 101, 0, 99, 0, 116, 0, 111, 0, 114, 0, 6, firewallDetectorPort[1], firewallDetectorPort[0], firewallDetectorAddress[0], firewallDetectorAddress[1], firewallDetectorAddress[2], firewallDetectorAddress[3], 0, 0, 0, 0],
                [81, 0, 0, 0, 5, 2, 0, 103, 0, 119, 14, 2, 6, 0, 0, 31, 0, 47, 0, 83, 0, 116, 0, 97, 0, 114, 0, 70, 0, 108, 0, 101, 0, 101, 0, 116, 0, 67, 0, 111, 0, 109, 0, 109, 0, 97, 0, 110, 0, 100, 0, 50, 0, 47, 0, 71, 0, 97, 0, 109, 0, 101, 0, 47, 0, 82, 0, 101, 0, 108, 0, 101, 0, 97, 0, 115, 0, 101, 0, 0, 0],
                [43, 0, 0, 0, 5, 2, 0, 3, 0, 0, 0, 128, 119, 14, 2, 6, 1, 0, 68, 7, 0, 82, 0, 101, 0, 108, 0, 101, 0, 97, 0, 115, 0, 101, 0, 0, 0, directoryServerTime[0], directoryServerTime[1], directoryServerTime[2], directoryServerTime[3], 0, 0]
            ];
        }

        public DuplexService15101(IPAddress address) : base(address, port: 15101, dataMinSize: 4, dataMaxSize: 512, dataDelimiter: null)
        {
            _processMessageMethod += ProcessMessageAsync;
        }

        private ValueTask<int> ProcessMessageAsync(DuplexServiceTransport client, ReadOnlySequence<byte> sequence)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(_dataMaxSize);

            try
            {
                client.Read(sequence, buffer, out Span<byte> msg);

                // checks the request

                if (msg.StartsWith(_data[0]))
                    msg = _data[1];
                else if (msg.StartsWith(_data[2]))
                    msg = _data[3];
                else
                    goto throwError;

                // sends the response

                client.Write(msg);

                return ValueTask.FromResult(msg.Length);

            throwError:

                return ValueTask.FromResult(-1);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }
}
