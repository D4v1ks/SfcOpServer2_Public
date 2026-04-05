using System.IO.Pipelines;

namespace shrNet
{
    public sealed class DuplexPipe(PipeReader reader, PipeWriter writer) : IDuplexPipe
    {
        public PipeReader Input { get; } = reader;
        public PipeWriter Output { get; } = writer;
    }
}
