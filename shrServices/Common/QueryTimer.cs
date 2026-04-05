using System;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;

namespace shrServices
{
    public static partial class QueryTimer
    {
        [LibraryImport("ntdll.dll", SetLastError = true)]
        private static partial int NtQueryTimerResolution(out uint minimumResolution, out uint maximumResolution, out uint currentResolution);

        /// <summary>
        /// Returns the current resolution in milliseconds.
        /// </summary>
        public static int CurrentResolution
        {
            get 
            {
                if (NtQueryTimerResolution(out uint minimumResolution, out uint maximumResolution, out uint currentResolution) == 0)
                {
                    Contract.Assert(minimumResolution >= maximumResolution);

                    if (currentResolution >= (uint)(TimeSpan.TicksPerMillisecond << 1))
                        return (int)(currentResolution / (uint)TimeSpan.TicksPerMillisecond);
                }
                
                return 1;
            }
        }
    }
}
