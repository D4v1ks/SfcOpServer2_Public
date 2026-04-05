using System;
using System.Collections.Concurrent;
using System.Threading;

namespace shrNet
{
    public sealed class DuplexQueue(bool isShared) : IDisposable
    {
        public readonly bool IsShared = isShared;

        private readonly ConcurrentQueue<DuplexMessage> _queue = new();

        private long _isDisposing;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _isDisposing, 1L) == 0L)
            {
                while (!_queue.IsEmpty)
                {
                    if (_queue.TryDequeue(out DuplexMessage msg))
                        msg.Release();
                }
            }

            GC.SuppressFinalize(this);
        }

        public bool TryEnqueue(DuplexMessage msg)
        {
            if (Interlocked.Read(ref _isDisposing) != 0L)
            {
                msg.Release();

                return false;
            }

            _queue.Enqueue(msg);

            return true;
        }

        public bool TryDequeue(out DuplexMessage msg)
        {
            if (Interlocked.Read(ref _isDisposing) != 0L)
            {
                msg = default;

                return false;
            }

            return _queue.TryDequeue(out msg);
        }
    }
}
