using System;
using System.Buffers;
using System.Diagnostics.Contracts;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace shrNet
{
    public abstract class DuplexService : DisposeAndForget
    {
        internal delegate ValueTask<int> HandshakeClient(DuplexServiceTransport client);
        internal delegate ValueTask<int> ProcessMessage(DuplexServiceTransport client, ReadOnlySequence<byte> sequence);

        protected DuplexListener _listener;

        internal readonly int _dataMinSize;
        internal readonly int _dataMaxSize;
        internal readonly byte[] _dataDelimiter;
        internal HandshakeClient _handshakeClientMethod;
        internal ProcessMessage _processMessageMethod;

        public DuplexService(IPAddress address, int port, int dataMinSize, int dataMaxSize, byte[] dataDelimiter)
        {
            _listener = new(address, port, ProcessSocketAsync);

            Contract.Assert((dataMinSize == 0 || dataMinSize == 2 || dataMinSize == 4) && (dataMinSize < dataMaxSize));

            _dataMinSize = dataMinSize;
            _dataMaxSize = dataMaxSize;

            Contract.Assert(
                (dataDelimiter == null && (dataMinSize == 2 || dataMinSize == 4)) ||
                (dataDelimiter != null && dataDelimiter.Length > 0 && dataMinSize == 0)
            );

            _dataDelimiter = dataDelimiter;
        }

        public void Start()
        {
            Contract.Assert(_processMessageMethod != null);

            _listener.StartAsync().FireAndForget();
        }

        protected override void OnClose()
        {
            DuplexDelegate.Unsubscribe(ref _handshakeClientMethod);
            DuplexDelegate.Unsubscribe(ref _processMessageMethod);
        }

        protected override void OnDispose()
        {
            if (_listener != null)
            {
                _listener.Dispose();
                _listener = null;
            }
        }

        private async Task ProcessSocketAsync(Socket socket)
        {
            DuplexServiceTransport client = null;

            try
            {
                DuplexSocket.Initialize(socket);

                client = new(this, socket);

                await client.StartAsync();
            }
            catch (Exception)
            { }
            finally
            {
                if (client != null)
                    client.Dispose();
                else
                    socket.Dispose();
            }
        }
    }
}
