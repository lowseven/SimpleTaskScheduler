using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using TPL.SimpleTaskScheduler;

namespace TPL.SimpleTaskSchedulerTest.Fakes
{
    public class TPLTaskSchedulerWithInvalidConsTaskFake : TPLTaskScheduler
    {
        public TPLTaskSchedulerWithInvalidConsTaskFake(
           ILogger logger
           , int consumersCount = TPLConstants.TPL_SCHEDULER_MIN_CONS_COUNT
           , int dueTime = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS
           , int maxQueueItems = TPLConstants.TPL_SCHEDULER_MAX_QUEUE_ITEMS)
           : base(logger, consumersCount, dueTime, maxQueueItems)
        {

        }

        protected override bool IsValidTask(Task task) => false;
    }

    public class TPLTaskSchedulerWithInvalidCanceledConsTaskFake : TPLTaskScheduler
    {
        public static string EXCEPTION_ERROR { get; set; }

        public TPLTaskSchedulerWithInvalidCanceledConsTaskFake(
           ILogger logger
           , int consumersCount = TPLConstants.TPL_SCHEDULER_MIN_CONS_COUNT
           , int dueTime = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS
           , int maxQueueItems = TPLConstants.TPL_SCHEDULER_MAX_QUEUE_ITEMS)
           : base(logger, consumersCount, dueTime, maxQueueItems)
        {

        }

        protected override bool IsValidTask(Task task) 
            => throw new OperationCanceledException(EXCEPTION_ERROR);
    }

    public class TPLTaskSchedulerWithCanceledConsTaskFake : TPLTaskScheduler
    {
        public TPLTaskSchedulerWithCanceledConsTaskFake(
           ILogger logger
           , int consumersCount = TPLConstants.TPL_SCHEDULER_MIN_CONS_COUNT
           , int dueTime = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS
           , int maxQueueItems = TPLConstants.TPL_SCHEDULER_MAX_QUEUE_ITEMS)
           : base(logger, consumersCount, dueTime, maxQueueItems)
        {

        }

        public void CancelConsumerTask()
        {
            foreach (var consTask in _ConsumerTaskList)
            {
                consTask.Item2.Cancel();
            }
        }
    }

    public class TPLTaskSchedulerCannotAddWorkItemsFake : TPLTaskScheduler
    {
        public TPLTaskSchedulerCannotAddWorkItemsFake(
           ILogger logger
           , int consumersCount = TPLConstants.TPL_SCHEDULER_MIN_CONS_COUNT
           , int dueTime = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS
           , int maxQueueItems = TPLConstants.TPL_SCHEDULER_MAX_QUEUE_ITEMS)
           : base(logger, consumersCount, dueTime, maxQueueItems)
        {

        }

        protected override bool IsOutOfRange() => true;
    }
}
