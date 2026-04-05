using System.Threading;

namespace shrNet
{
    public static class DuplexId
    {
        private static int _uniqueId = 0;

        public static int GetUniqueId()
        {
            return Interlocked.Increment(ref _uniqueId);
        }
    }
}
