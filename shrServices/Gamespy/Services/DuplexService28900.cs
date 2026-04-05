using shrNet;

using System;
using System.Buffers;
using System.Net;
using System.Threading.Tasks;

namespace shrServices
{
    public sealed class DuplexService28900 : DuplexService
    {
        private enum Args
        {
            Final,

            Gamename,
            Gamever,
            List
        }

        private readonly static byte[][] _args =
        [
            "\\final\\"u8.ToArray(),

            "\\gamename\\"u8.ToArray(),
            "\\gamever\\"u8.ToArray(),
            "\\list\\"u8.ToArray()
        ];

        private readonly static byte[] _response =
            "\\basic\\\\secure\\000000"u8.ToArray();

        public DuplexService28900(IPAddress address) : base(address, port: 28900, dataMinSize: 0, dataMaxSize: 1024, _args[(int)Args.Final])
        {
            _handshakeClientMethod += HandshakeClientAsync;
            _processMessageMethod += ProcessMessageAsync;
        }

        private ValueTask<int> HandshakeClientAsync(DuplexServiceTransport client)
        {
            client.Write(_response);

            return ValueTask.FromResult(_response.Length);
        }

        private ValueTask<int> ProcessMessageAsync(DuplexServiceTransport client, ReadOnlySequence<byte> sequence)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(_dataMaxSize);

            try
            {
                client.Read(sequence, buffer, out Span<byte> msg);

                // checks the request

                if (Utils.TryGetValue(msg, _args[(int)Args.Gamename], 92, out string gameName) && gameName.Equals(GamespyService.GameName, StringComparison.Ordinal))
                {
                    if (Utils.TryGetValue(msg, _args[(int)Args.Gamever], 92, out string gameVersion) && gameVersion.Equals(GamespyService.GameVersion, StringComparison.Ordinal))
                    {
                        /*
                            \gamename\sfc2op\gamever\1.6\location\0\validate\Dvz0jxQz\final\
                        */

                        goto skipResponse;
                    }

                    if (msg.IndexOf(_args[(int)Args.List]) >= 0)
                    {
                        /*
                            \queryid\1.1\\list\cmp\gamename\sfc2op\final\
                        */

                        msg = buffer;

                        GamespyService.ListGames(ref msg);
                        Utils.Append(ref msg, _args[(int)Args.Final]);

                        msg = buffer.AsSpan()[..^msg.Length];

                        goto sendResponse;
                    }
                }

                return ValueTask.FromResult(-1);

            skipResponse:

                return ValueTask.FromResult(0);

            sendResponse:

                client.Write(msg);

                return ValueTask.FromResult(msg.Length);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }
}
