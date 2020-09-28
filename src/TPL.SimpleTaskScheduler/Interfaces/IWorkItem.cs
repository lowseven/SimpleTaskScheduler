using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace TPL.Interfaces
{
    public interface IWorkItem<TData> where TData : class
    {
        /// <summary>
        /// Returns a Task with its result
        /// </summary>
        Task<TData> Task { get; }
    }

    public interface IWorkItem : IAwaitable<IWorkItem>, IDisposable
    {
        /// <summary>
        /// The ID of the work. The scheduler will set it automatically
        /// </summary>
        int Id { get; }
        /// <summary>
        /// Check if the workItem is runnable and not faulted or canceled
        /// </summary>
        bool IsValid { get; }
        /// <summary>
        /// Returns the task representation of the work
        /// </summary>
        Task Task { get; }
        /// <summary>
        /// The work that will be completed
        /// </summary>
        Action DoWork { get; }
        /// <summary>
        /// Cancel the work
        /// </summary>
        void SetCanceled();
        /// <summary>
        /// Sets the completion and the result of the work.
        /// It can be retrieved by casting the WorkItem.Task to WorkItem.Task<TYPE>
        /// </summary>
        /// <param name="result">The result of the work</param>
        void SetCompletion(object result);
        /// <summary>
        /// Capture and store the exception in the WorkItem.Task.Exception
        /// </summary>
        /// <param name="ex"></param>
        void SetException(Exception ex);
    }
}
