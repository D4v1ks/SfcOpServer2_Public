using shrNet;
using System.Net;

namespace shrServices
{
    public sealed class IrcProfile : IIrcProfile
    {
        public int Id { get; } = DuplexId.GetUniqueId();

        public string LocalIP { get; } = IPAddress.Loopback.ToString();
        public string Nick { get; set; }
        public string Name { get; set; }
        public string User { get; set; }
        public long Modes { get; set; }

        public long LastTick { get; set; }
    }
}
