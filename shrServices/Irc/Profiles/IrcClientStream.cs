using shrNet;

using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Text;
using System.Threading;

namespace shrServices
{
    public sealed class IrcClientStream : IIrcClient
    {
        private readonly IrcProfile _profile;
        private readonly DuplexQueue _inboundQueue;
        private readonly DuplexQueue _outboundQueue;

        private long _closing;

        public bool IsDisposing => Interlocked.Read(ref _closing) != 0;

        public int Id => _profile.Id;
        public string LocalIP => _profile.LocalIP;
        public string Nick
        {
            get
            {
                return _profile.Nick;
            }
            set
            {
                _profile.Nick = value;
            }
        }
        public string Name
        {
            get
            {
                return _profile.Name;
            }
            set
            {
                _profile.Name = value;
            }
        }
        public string User
        {
            get
            {
                return _profile.User;
            }
            set
            {
                _profile.User = value;
            }
        }
        public long Modes
        {
            get
            {
                return _profile.Modes;
            }
            set
            {
                _profile.Modes = value;
            }
        }
        public long LastTick
        {
            get
            {
                return _profile.LastTick;
            }
            set
            {
                _profile.LastTick = value;
            }
        }

        public IrcClientStream(IrcProfile profile, DuplexQueue inboundQueue, DuplexQueue outboundQueue)
        {
            _profile = profile;

            Contract.Assert(inboundQueue == null || inboundQueue.IsShared == false);

            _inboundQueue = inboundQueue;

            Contract.Assert(outboundQueue != null);

            _outboundQueue = outboundQueue;
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _closing, 1) == 0)
            {
                if (_inboundQueue != null)
                {
                    Contract.Assert(!_inboundQueue.IsShared);

                    _inboundQueue.Dispose();
                }

                if (!_outboundQueue.IsShared)
                    _outboundQueue.Dispose();
            }

            GC.SuppressFinalize(this);
        }

        public bool TryRead(out DuplexMessage msg)
        {
            bool dequeuedMsg = _inboundQueue.TryDequeue(out msg);

#if VERBOSE
            if (dequeuedMsg)
                LogMessage(msg, _inboundLog, isFromClient: false);
#endif

            return dequeuedMsg;
        }

        public bool TryWrite(Span<byte> span)
        {
            Contract.Assert(!span.IsEmpty);

            DuplexMessage msg = new(Id, span.Length);

            span.CopyTo(msg.AsSpan());

            bool enqueuedMsg = _outboundQueue.TryEnqueue(msg);

#if VERBOSE
            if (enqueuedMsg)
                LogMessage(msg, _outboundLog, isFromClient: true);
#endif

            return enqueuedMsg;
        }

#if VERBOSE
        private readonly StringBuilder _outboundLog = new(IrcService.MaximumBufferSize);
        private readonly StringBuilder _inboundLog = new(IrcService.MaximumBufferSize);

        private void LogMessage(DuplexMessage msg, StringBuilder log, bool isFromClient)
        {
            Contract.Assert(log.Length == 0);

            log.Append(DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture));

            if (isFromClient)
                log.Append(" [Client");
            else
                log.Append(" [server");

            log.Append(Id);
            log.Append("] ");

            int size = msg.AsReadOnlySpan().IndexOf(new ReadOnlySpan<byte>(IrcService.Delimiter));

            Contract.Assert(size != -1);

            size += IrcService.Delimiter.Length;

            log.Append(Encoding.UTF8.GetString(new ReadOnlySpan<byte>(msg.Buffer, 0, size)));

            Debug.Write(log.ToString());

            log.Clear();
        }
#endif

    }
}
