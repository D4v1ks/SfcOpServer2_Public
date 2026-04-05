using shrNet;

using System;
using System.Net;
using System.Net.Sockets;

namespace shrServices
{
    public sealed class IrcClientSocket(Socket socket, DuplexQueue inboundQueue) : IIrcClient
    {
        private readonly DuplexClientTransport _duplexClientTransport = new(socket, inboundQueue, dataMinSize: 0, IrcService.MaximumBufferSize, IrcService.Delimiter);
        private readonly string _localIP = ((IPEndPoint)socket.RemoteEndPoint).Address.ToString();

        public DuplexClientTransport Transport => _duplexClientTransport;

        public bool IsDisposing => _duplexClientTransport.IsDisposing;

        public int Id => _duplexClientTransport.Id;
        public string LocalIP => _localIP;
        public string Nick { get; set; }
        public string Name { get; set; }
        public string User { get; set; }
        public long Modes { get; set; }
        public long LastTick { get; set; }

        public void Dispose()
        {
            _duplexClientTransport.Dispose();

            GC.SuppressFinalize(this);
        }

        public bool TryRead(out DuplexMessage msg)
        {
            return _duplexClientTransport.TryRead(out msg);
        }

        public bool TryWrite(Span<byte> span)
        {
            return _duplexClientTransport.TryWrite(span);
        }
    }
}
