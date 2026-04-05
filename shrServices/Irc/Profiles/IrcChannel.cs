using System.Collections.Generic;

namespace shrServices
{
    public sealed class IrcChannel(string name)
    {
        public string Name { get; } = name;
        public Dictionary<int, object> Clients { get; } = new(IrcService.MininumChannelSize);
        public long Modes { get; set; }
    }
}
