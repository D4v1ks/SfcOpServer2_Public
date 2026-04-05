using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace shrNet
{
    public sealed class DuplexListener : DisposeAndForget
    {
        public delegate Task ProcessSocket(Socket socket);

        private readonly IPEndPoint _localEP;

        private ProcessSocket _processSocketMethod;
        private Socket _listener;

        public DuplexListener(IPAddress address, int port, ProcessSocket processSocketMethod)
        {
            _localEP = new IPEndPoint(address, port);

            Contract.Assert(processSocketMethod != null);

            _processSocketMethod += processSocketMethod;
        }

        public async Task StartAsync()
        {
            try
            {
                Contract.Assert(_listener == null);

                _listener = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
                {
                    ExclusiveAddressUse = true
                };

                _listener.Bind(_localEP);
                _listener.Listen();

#if VERBOSE
                Debug.WriteLine("Started listening at " + _localEP.ToString());
#endif

                while (true)
                {
                    Socket socket = await _listener.AcceptAsync();

                    _processSocketMethod(socket).FireAndForget();
                }
            }
            catch (Exception)
            { }
            finally
            {

#if VERBOSE
                Debug.WriteLine("Stopped listening at " + _localEP.ToString());
#endif

            }
        }

        protected override void OnClose()
        {
            DuplexDelegate.Unsubscribe(ref _processSocketMethod);
        }

        protected override void OnDispose()
        {
            _listener?.Dispose();
        }
    }
}
