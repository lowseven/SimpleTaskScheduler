using System;

namespace TPL.SimpleTaskScheduler.Interfaces
{
    public interface ITaskScheduler<TData> : IDisposable where TData : class
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
        /// Queueing a WorItem into the scheduler
        /// </summary>
        /// <param name="doWork">The work to be done</param>
        void EnqueueWork(Func<TData> doWork);
        /// <summary>
        /// Queueing a WorItem into the scheduler
        /// </summary>
        /// <param name="doWork">The work to be executed</param>
        /// <param name="doWorkCallback">The work result callback.The workitem result and state will be passed in the arguments</param>
        void EnqueueWork(Func<TData> doWork, Action<TData, object> doWorkCallback = null);
        /// <summary>
        /// Determines whether the provided doWorkCallback can be executed 
        /// in this call, and if so, executes it.
        /// <param name="doWork">The work to be executed</param>
        /// </summary>
        bool TryExecuteWorkNow(Func<TData> doWork);
        /// <summary>
        /// Determines whether the provided doWorkCallback can be executed 
        /// in this call, and if so, executes it.
        /// <param name="doWork">The work to be executed</param>
        /// <param name="doWorkCallback">The work result callback</param>
        /// </summary>
        bool TryExecuteWorkNow(Func<TData> doWork, Action<TData, object> doWorkCallback);
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
        /// Queueing a WorItem into the scheduler
        /// </summary>
        /// <param name="doWork">The work to be done</param>
        void EnqueueWork(Action doWork);
        /// <summary>
        /// Queueing a WorItem into the scheduler
        /// </summary>
        /// <param name="doWork">The work to be executed</param>
        /// <param name="doWorkCallback">The work result callback. The workitem state will be passed in the argument</param>
        void EnqueueWork(Action doWork, Action<object> doWorkCallback = null);
        /// <summary>
        /// Determines whether the provided doWorkCallback can be executed 
        /// in this call, and if so, executes it.
        /// <param name="doWork">The work to be executed</param>
        /// </summary>
        bool TryExecuteWorkNow(Action doWork);
        /// <summary>
        /// Determines whether the provided doWorkCallback can be executed 
        /// in this call, and if so, executes it.
        /// <param name="doWork">The work to be executed</param>
        /// <param name="doWorkCallback">The work result callback</param>
        /// </summary>
        bool TryExecuteWorkNow(Action doWork, Action<object> doWorkCallback);
    }
}
