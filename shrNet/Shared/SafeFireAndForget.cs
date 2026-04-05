#define STACK_FRIENDLY_VERSION

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace shrNet
{
    public static class SafeFireAndForget
    {
        private static readonly Action<Exception> _onRethrowException = e => throw e;

#if STACK_FRIENDLY_VERSION
        /// <summary>
        /// Safely executes the Task, without waiting for it to complete, before moving to the next line of code.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void FireAndForget(this Task task, in bool rethrowException = true, in bool continueOnCapturedContext = true)
        {
            _ = Await(task, rethrowException, continueOnCapturedContext);
        }

        private static async Task Await(Task task, bool rethrowException, bool continueOnCapturedContext)
        {
            try
            {
                await task.ConfigureAwait(continueOnCapturedContext);
            }
            catch (Exception e)
            {
                if (rethrowException)
                    _onRethrowException.Invoke(e);
            }
        }
#else
        /// <summary>
        /// Safely executes the Task, without waiting for it to complete, before moving to the next line of code.
        /// </summary>
        public static void FireAndForget(this Task task, in bool rethrowException = true, in bool continueOnCapturedContext = true)
        {
            Await(task, rethrowException, continueOnCapturedContext);
        }

        private static async void Await(Task task, bool rethrowException, bool continueOnCapturedContext)
        {
            try
            {
                await task.ConfigureAwait(continueOnCapturedContext);
            }
            catch (Exception e)
            {
                if (rethrowException)
                    _onRethrowException.Invoke(e);
            }
        }
#endif

    }
}
