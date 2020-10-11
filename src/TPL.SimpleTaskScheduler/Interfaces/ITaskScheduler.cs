using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TPL.SimpleTaskScheduler;

namespace TPL.Interfaces
{
    public interface ITaskScheduler<TData> : ITaskScheduler where TData : class
    {
        /// <summary>
        /// Queueing a WorItem into the scheduler
        /// </summary>
        /// <param name="doWork">The work to be done</param>
        /// <param name="creationOptions">The optional creation options of the workItem</param>
        /// <param name="dueTime">The workitem timeout to complete the workItem</param>
        void EnqueueWork(Func<TData> doWork, TaskCreationOptions creationOptions = TaskCreationOptions.None, int dueTime = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS);
        /// <summary>
        /// Queueing a WorItem into the scheduler
        /// </summary>
        /// <param name="doWork">The work to be done</param>
        /// <param name="doWorkCallback">The optional method called after WorkItem completes its work</param>
        /// <param name="creationOptions">The optional creation options of the workItem</param>
        /// <param name="dueTime">The workitem timeout to complete the workItem</param>
        void EnqueueWork(Func<TData> doWork, Action<TData> doWorkCallback, TaskCreationOptions creationOptions = TaskCreationOptions.None, int dueTime = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS);
        /// <summary>
        /// Determines whether the provided doWorkCallback can be executed 
        /// in this call, and if so, executes it.
        /// <param name="doWork">The work to be executed</param>
        /// <param name="creationOptions">The optional creation options of the workItem</param>
        /// <param name="dueTime">The workitem timeout to complete the workItem</param>
        /// </summary>
        bool TryExecuteWorkNow(Func<TData> doWork, TaskCreationOptions creationOptions = TaskCreationOptions.None, int dueTime = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS);
        /// <summary>
        /// Determines whether the provided doWorkCallback can be executed 
        /// in this call, and if so, executes it.
        /// <param name="doWork">The work to be done</param>
        /// <param name="doWorkCallback">The optional method called after WorkItem completes its work</param>
        /// <param name="creationOptions">The optional creation options of the workItem</param>
        /// <param name="dueTime">The workitem timeout to complete the workItem</param>
        /// </summary>
        bool TryExecuteWorkNow(Func<TData> doWork, Action<TData> doWorkCallback = null, TaskCreationOptions creationOptions = TaskCreationOptions.None, int dueTime = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS);
    }

    public interface ITaskScheduler : IDisposable
    {
        /// <summary>
        /// The maximum concurrent operation this schedule can support
        /// </summary>
        int MaximumConcurrencyLevel { get; }
        /// <summary>
        /// Check if all task are completed
        /// </summary>
        bool AllTasksCompleted { get; }
        /// <summary>
        /// Get the snapshot of the current enqueued workitems
        /// </summary>
        IEnumerable<IWorkItem> EnqueuedWorkItems {get;}
        /// <summary>
        /// Queueing a callback as a workItem into the scheduler
        /// </summary>
        /// <param name="doWork">The work to be done</param>
        void EnqueueWork(Action doWork, TaskCreationOptions creationOptions = TaskCreationOptions.None, int dueTime = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS);
        /// <summary>
        /// Queueing a callback as a workItem into the scheduler
        /// aling with a onDone work callback
        /// </summary>
        /// <param name="doWork">The work to be executed</param>
        /// <param name="doWorkCallback">The work result callback. The workitem state will be passed in the argument</param>
        /// <param name="creationOptions">The optional creation options of the workItem</param>
        /// <param name="dueTime">The workitem timeout to complete the workItem</param>
        void EnqueueWork(Action doWork, Action doWorkCallback, TaskCreationOptions creationOptions = TaskCreationOptions.None, int dueTime = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS);
        /// <summary>
        /// Queueing a WorItem into the scheduler
        /// </summary>
        /// <param name="work">the work to be executed</param>
        void EnqueueWork(IWorkItem work);
        /// <summary>
        /// Queueing a WorItem list into the scheduler
        /// </summary>
        /// <param name="work">the work to be executed</param>
        void EnqueueWork(IEnumerable<IWorkItem> works);
        /// <summary>
        /// Determines whether the provided doWorkCallback can be executed 
        /// in this call, and if so, executes it.
        /// <param name="work">The work to be executed</param>
        /// </summary>
        bool TryExecuteItNow(IWorkItem work);
        /// <summary>
        /// Determines whether the provided doWorkCallback can be executed 
        /// in this call, and if so, executes it.
        /// <param name="doWork">The work to be executed</param>
        /// <param name="creationOptions">The optional creation options of the workItem</param>
        /// <param name="dueTime">The workitem timeout to complete the workItem</param>
        /// </summary>
        bool TryExecuteItNow(Action doWork, TaskCreationOptions creationOptions = TaskCreationOptions.None, int dueTime = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS);
        /// <summary>
        /// Determines whether the provided doWorkCallback can be executed 
        /// in this call, and if so, executes it.
        /// <param name="doWork">The work to be executed</param>
        /// <param name="doWorkCallback">The work result callback</param>
        /// <param name="creationOptions">The optional creation options of the workItem</param>
        /// <param name="dueTime">The workitem timeout to complete the workItem</param>
        /// </summary>
        bool TryExecuteItNow(Action doWork, Action doWorkCallback, TaskCreationOptions creationOptions = TaskCreationOptions.None, int dueTime = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS);
    }
}
