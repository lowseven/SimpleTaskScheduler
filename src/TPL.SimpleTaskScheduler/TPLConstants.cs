using System;
using System.Collections.Generic;
using System.Text;

namespace TPL.SimpleTaskScheduler
{
    public static class TPLConstants
    {
        public const int TPL_SCHEDULER_SECONDS_MULTI = 1000;
        public const int TPL_SCHEDULER_MIN_WAIT_SECONDS = 5;
        internal const int TPL_SCHEDULER_MIN_THREAD_COUNT = 3;
    }
}
