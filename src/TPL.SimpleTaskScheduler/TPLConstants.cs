namespace TPL.SimpleTaskScheduler
{
    public static class TPLConstants
    {
        public const int TPL_SCHEDULER_SECONDS_MULTI = 1000;
        public const int TPL_SCHEDULER_MIN_THREAD_COUNT = 3;
        public const int TPL_SCHEDULER_MIN_WAIT_SECONDS = 5;
        public const int TPL_SCHEDULER_MAX_QUEUE_ITEMS = -1; //infinite
        public const string TPL_SCHEDULER_MAX_QUEUE_ITEMS_EX = "Maximum WorkItems queue count reached {0}";
    }
}
