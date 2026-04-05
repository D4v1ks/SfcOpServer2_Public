using System;
using System.Buffers;
using System.Diagnostics.Contracts;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace shrNet
{
    public abstract class DuplexApplication : DisposeAndForget
    {
        protected readonly int _id;
        protected readonly Socket _socket;
        protected readonly DuplexPipe _inboundPipe;
        protected readonly DuplexPipe _outboundPipe;

        protected SocketError _socketReceiveError;

        public int Id => _id;

        public DuplexApplication(Socket socket)
        {
            _id = DuplexId.GetUniqueId();

            Contract.Assert(socket != null);

            _socket = socket;

            Pipe inboundPipe = new(PipeOptions.Default);
            Pipe outboundPipe = new(PipeOptions.Default);

            _inboundPipe = new(inboundPipe.Reader, outboundPipe.Writer);
            _outboundPipe = new(outboundPipe.Reader, inboundPipe.Writer);

            Contract.Assert(_socketReceiveError == SocketError.Success);
        }

        public abstract Task StartAsync();

        protected async Task InboundReadAsync()
        {
            PipeReader reader = _inboundPipe.Input;

            try
            {
                while (true)
                {
                    ReadResult result = await reader.ReadAsync();
                    ReadOnlySequence<byte> buffer = result.Buffer;

                    try
                    {
                        if (result.IsCanceled)
                            break;

                        if (!buffer.IsEmpty)
                        {
                            try
                            {
                                if (buffer.IsSingleSegment)
                                    await _socket.SendAsync(buffer.First, SocketFlags.None);
                                else
                                {
                                    SequencePosition position = buffer.Start;

                                    buffer.TryGet(ref position, out ReadOnlyMemory<byte> curSegment);

                                    while (buffer.TryGet(ref position, out ReadOnlyMemory<byte> nextSegment))
                                    {
                                        await _socket.SendAsync(curSegment, SocketFlags.None);

                                        curSegment = nextSegment;
                                    }

                                    await _socket.SendAsync(curSegment, SocketFlags.None);
                                }
                            }
                            catch (Exception)
                            {
                                break;
                            }
                        }
                        else if (result.IsCompleted)
                            break;
                    }
                    finally
                    {
                        reader.AdvanceTo(buffer.End);
                    }
                }
            }
            catch (Exception)
            { }
            finally
            {
                reader.Complete();

                Close();
            }
        }

        protected async Task InboundWriteAsync()
        {
            PipeWriter writer = _inboundPipe.Output;

            try
            {
                while (true)
                {
                    await _socket.ReceiveAsync(Memory<byte>.Empty, SocketFlags.None);

                    Memory<byte> buffer = writer.GetMemory(4096);

                    int bytesReceived = await _socket.ReceiveAsync(buffer, SocketFlags.None);

                    if (bytesReceived == 0)
                        break;

                    writer.Advance(bytesReceived);

                    FlushResult result = await writer.FlushAsync();

                    if (result.IsCanceled || result.IsCompleted)
                        break;
                }
            }
            catch (SocketException e)
            {
                _socketReceiveError = e.SocketErrorCode;
            }
            catch (Exception)
            { }
            finally
            {
                writer.Complete();

                Close();
            }
        }

        protected override void OnClose()
        {
            if (_socketReceiveError != SocketError.ConnectionReset)
            {
                try
                {
                    _socket.Disconnect(false);
                }
                catch (Exception)
                { }
            }

            _inboundPipe.Input.CancelPendingRead();
            _inboundPipe.Output.CancelPendingFlush();
        }

        protected override void OnDispose()
        {
            _socket.Dispose();
        }
    }
}
