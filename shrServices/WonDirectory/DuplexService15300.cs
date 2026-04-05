//#define RUN_FIREWALL_TEST

using shrNet;

using System;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace shrServices
{
    public sealed class DuplexService15300 : DuplexService
    {
        private readonly static byte[][] _data =
        [
            [15, 0, 5, 3, 0, 1, 0],
            [1, 32, 78, 0, 0, 0],
            [9, 0, 5, 3, 0, 2, 0, 0, 0],
            [9, 0, 5, 3, 0, 2, 0, 255, 255]
        ];

        public DuplexService15300(IPAddress address) : base(address, port: 15300, dataMinSize: 2, dataMaxSize: 512, dataDelimiter: null)
        {
            _processMessageMethod += ProcessMessageAsync;
        }

        private async ValueTask<int> ProcessMessageAsync(DuplexServiceTransport client, ReadOnlySequence<byte> sequence)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(_dataMaxSize);

            try
            {
                client.Read(sequence, buffer, out Span<byte> msg);

                // checks the request

                if (msg.StartsWith(_data[0]) && msg.EndsWith(_data[1]))
                {

#if RUN_FIREWALL_TEST
                    Socket socket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
#endif

                    try
                    {
                        int port = BitConverter.ToUInt16(buffer, 7);

                        if (port >= 2300 && port <= 2309 || port == 27001 || port == 47624)
                        {

#if RUN_FIREWALL_TEST
                            await socket.ConnectAsync(client.RemoteIPAddress, port);
#endif

                            msg = _data[2];
                        }
                        else
                            msg = _data[3];
                    }
                    catch (Exception)
                    {
                        msg = _data[3];
                    }

#if RUN_FIREWALL_TEST
                    finally
                    {
                        socket?.Dispose();
                    }
#endif

                }
                else
                    goto throwError;

                // sends the response

                client.Write(msg);

                return await ValueTask.FromResult(msg.Length);

            throwError:

                return await ValueTask.FromResult(-1);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }
}
