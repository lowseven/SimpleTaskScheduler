using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace TPL.Interfaces
{
    public interface IWorkItem<TData> : IWorkItem where TData : class
    {
        /// <summary>
        /// Returns a Task with its result
        /// </summary>
        new Task<TData> Task { get; }
    }

    public interface IWorkItem : IAwaitable<IWorkItem>, IDisposable
    {
        /// <summary>
        /// The ID of the work. The scheduler will set it automatically
        /// </summary>
        int Id { get; }
        /// <summary>
        /// Checks if a workItem is runnable and not faulted or canceled
        /// </summary>
        bool IsValid { get; }
        /// <summary>
        /// Checks if a WorkItem can be started.
        /// </summary>
        bool IsRunnable { get; }
        /// <summary>
        /// Check if the work item is canceled
        /// </summary>
        bool IsCanceled { get; }
        /// <summary>
        /// Returns the task representation of the work
        /// </summary>
        Task Task { get; }
        /// <summary>
        /// The work that will be completed
        /// </summary>
        Action DoWork { get; }
        /// <summary>
        /// Set the work as cancelled
        /// </summary>
        void SetCanceled();
        /// <summary>
        /// Sets the completion and the result of the work.
        /// It can be retrieved by casting the WorkItem.Task to WorkItem.Task<TYPE>
        /// </summary>
        void SetResult();
        /// <summary>
        /// Set the workItem as faulted and then Capture 
        /// the exception in the WorkItem.Task.Exception
        /// </summary>
        /// <param name="ex">The exception</param>
        void SetException(Exception ex);
        /// <summary>
        /// Notify a cancellation request
        /// </summary>
        void NotifyCancellation();
    }
}
