namespace shrNet
{
    public interface IClient
    {
        public bool TryRead(out byte[] buffer);
        public void Write(byte[] buffer, int offset, int size);
    }
}
