using shrNet;
using shrServices;

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace shrWire
{
    internal sealed class WireServer : GamespyGame
    {
        private const int CentralSwitchPort = 27000;

        private readonly IrcService _ircService;
        private readonly TcpListener _listener;

        public WireServer(IPAddress localIP) : base(localIP, CentralSwitchPort)
        {
            _hostName = "Standard";
            _earlyMap = "H&S.mvm";
            _middleMap = "H&S.mvm";
            _lateMap = "H&S.mvm";
            _gameType = "A stock multiplayer campaign (ManInTheMiddle)";
            _maxNumPlayers = 100;
            _raceList = 0b_00000000_11111111_11111111;

            _ircService = new IrcService(localIP, 27003, null, null);
            _listener = new(localIP, CentralSwitchPort);
        }

        public async Task StartAsync()
        {
            try
            {
                AdvertiseAsync().FireAndForget();

                _ircService.StartAsync().FireAndForget();
                _listener.Start();

                while (true)
                {
                    TcpClient tcpClient = await _listener.AcceptTcpClientAsync().ConfigureAwait(false);

                    new WireClient().StartAsync(tcpClient).FireAndForget();
                }
            }
            catch (Exception)
            { }
            finally
            {
                Dispose();
            }
        }

        protected override void TryDispose()
        {
            base.TryDispose();

            _ircService?.Dispose();
            _listener?.Stop();
        }
    }
}
