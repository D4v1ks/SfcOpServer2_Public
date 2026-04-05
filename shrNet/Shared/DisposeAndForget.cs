using System;
using System.Diagnostics.Contracts;
using System.Threading;

namespace shrNet
{
    public abstract class DisposeAndForget : IDisposable
    {
        private const long isRunning = 0;
        private const long isClosing = 1;
        private const long isClosed = 2;
        private const long isDisposing = 4;
        private const long isDisposed = 8;

        private long _flags;

        public bool IsDisposing => Interlocked.Read(ref _flags) != isRunning;

        public void Dispose()
        {
            // loops until the object is correctly closed and disposed

            SpinWait spin = new();

            while (true)
            {
                long temp = Interlocked.CompareExchange(ref _flags, isClosing | isDisposing, isRunning);

                if (temp != isRunning)
                {
                    if (temp >= isDisposing)
                    {
                        Contract.Assert(temp == (isClosing | isDisposing) || temp == (isClosed | isDisposing) || temp == (isClosed | isDisposed));

                        break;
                    }

                    temp = Interlocked.CompareExchange(ref _flags, isClosing | isDisposing, isClosing);

                    if (temp != isClosing)
                    {
                        if (temp >= isDisposing)
                        {
                            Contract.Assert(temp == (isClosing | isDisposing) || temp == (isClosed | isDisposing) || temp == (isClosed | isDisposed));

                            break;
                        }

                        temp = Interlocked.CompareExchange(ref _flags, isClosed | isDisposing, isClosed);

                        if (temp != isClosed)
                        {
                            if (temp >= isDisposing)
                            {
                                Contract.Assert(temp == (isClosing | isDisposing) || temp == (isClosed | isDisposing) || temp == (isClosed | isDisposed));

                                break;
                            }

                            Contract.Assert(temp <= isClosed);

                            spin.SpinOnce();

                            continue;
                        }
                    }
                    else
                    {
                        while (true)
                        {
                            temp = Interlocked.Read(ref _flags);

                            if (temp != (isClosing | isDisposing))
                            {
                                Contract.Assert(temp == (isClosed | isDisposing));

                                break;
                            }

                            spin.SpinOnce();
                        }
                    }
                }
                else
                {
                    OnClose();
                }

                OnDispose();

                Interlocked.Exchange(ref _flags, isClosed | isDisposed);

                break;
            }

            GC.SuppressFinalize(this);
        }

        public void Close()
        {
            if (Interlocked.CompareExchange(ref _flags, isClosing, isRunning) == isRunning)
            {
                OnClose();

                Interlocked.Increment(ref _flags);
            }
        }

#if DEBUG
        internal void ResetFlags()
        {
            Interlocked.CompareExchange(ref _flags, isRunning, isClosed | isDisposed);
        }
#endif

        protected abstract void OnClose();
        protected abstract void OnDispose();
    }
}
