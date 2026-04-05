using shrNet;

using System;
using System.Buffers;
using System.Net;
using System.Threading.Tasks;

namespace shrServices
{
    public sealed class DuplexService29901 : DuplexService
    {
        private enum Args
        {
            Final,

            Valid,
            Email
        }

        private readonly static byte[][] _args =
        [
            "\\final\\"u8.ToArray(),

            "\\valid\\"u8.ToArray(),
            "\\email\\"u8.ToArray()
        ];

        private readonly static byte[][] _responses =
        [
            "\\vr\\1\\final\\"u8.ToArray(),
            "\\vr\\0\\final\\"u8.ToArray()
        ];

        public DuplexService29901(IPAddress address) : base(address, port: 29901, dataMinSize: 0, dataMaxSize: 1024, _args[(int)Args.Final])
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

                if (msg.StartsWith(_args[(int)Args.Valid]) && Utils.TryGetValue(msg, _args[(int)Args.Email], 92, out string email))
                {
                    /*
                        \valid\\email\d4v1ks@hotmail.com\final\
                    */

                    if (GamespyService.ContainsEmail(email))
                        msg = _responses[0];
                    else
                        msg = _responses[1];

                    goto sendResponse;
                }

                return ValueTask.FromResult(-1);

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
