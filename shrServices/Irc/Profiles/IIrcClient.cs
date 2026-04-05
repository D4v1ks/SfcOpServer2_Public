using shrNet;
using System;

namespace shrServices
{
    public interface IIrcClient : IDisposable, IIrcProfile
    {
        bool IsDisposing { get; }

        bool TryRead(out DuplexMessage msg);
        bool TryWrite(Span<byte> span);
    }
}
