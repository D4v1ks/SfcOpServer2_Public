using System;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

namespace shrNet
{
    public static class DuplexDelegate
    {
        public static void Unsubscribe<T>(ref T source) where T : MulticastDelegate
        {
            if (source != null)
            {

#if DEBUG
                Contract.Assert(InvocationCount(source) == IntPtr.Zero && InvocationList(source) is null);
#endif

                foreach (Delegate entry in source.GetInvocationList())
                    source = (T)Delegate.Remove(source, (T)entry);
            }
        }

#if DEBUG
        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_invocationCount")]
        private static extern ref IntPtr InvocationCount(MulticastDelegate source);

        [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_invocationList")]
        private static extern ref object InvocationList(MulticastDelegate source);
#endif

    }
}
