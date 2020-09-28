using System;
using System.Threading;
using System.Threading.Tasks;
using TPL.Interfaces;

namespace TPL.SimpleTaskScheduler
{
    /// <summary>
    /// Represents the a Work that will be processed,
    /// by default the canceling time is 5 seconds
    /// </summary>
    /// <typeparam name="TData">The dataType of the work result</typeparam>
    public class WorkItem<TData> : WorkItem, IWorkItem<TData> where TData : class
    {
        public WorkItem(
            Func<TData> doWork
            , TaskCreationOptions options = TaskCreationOptions.None
            , int secsBeforeCanceling = 5) : base(options, secsBeforeCanceling)
        {
            if (doWork is null) throw new ArgumentNullException(nameof(doWork));
            if (secsBeforeCanceling <= 0) throw new ArgumentNullException(nameof(secsBeforeCanceling));

            _DoWork = () => { SetCompletition(doWork); };
            _Disposed = false;
            _TaskSource.Task.ConfigureAwait(false);

            _CancellationSource.Token.Register(() => SetCanceled(), false);
        }

        public new Task<TData> Task => this._TaskSource.Task as Task<TData>;
    }

    /// <summary>
    /// Represents the a Work that will be processed,
    /// by default the canceling time is 5 seconds
    /// </summary>
    public class WorkItem : IWorkItem
    {
        public int Id { get => _TaskSource.Task.Id; }
        public bool IsValid { get => _CancellationSource.IsCancellationRequested || _TaskSource.Task.IsCanceled || _TaskSource.Task.IsFaulted; }

        public Task Task => _TaskSource.Task;
        public Action DoWork => _DoWork;
        public void Dispose() => Dispose(true);

        protected bool _Disposed;
        protected Action _DoWork;
        protected readonly int _SecsBeforeCancellingMe;
        protected readonly CancellationTokenSource _CancellationSource;
        protected readonly TaskCompletionSource<object> _TaskSource;

        internal WorkItem(
             TaskCreationOptions options = TaskCreationOptions.None
           , int secsBeforeCanceling = 5)
        {
            if (secsBeforeCanceling <= 0) throw new ArgumentNullException(nameof(secsBeforeCanceling));

            _SecsBeforeCancellingMe = secsBeforeCanceling * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI;
            _CancellationSource = new CancellationTokenSource(_SecsBeforeCancellingMe);
            _TaskSource = new TaskCompletionSource<object>(options);
            _Disposed = false;

            _CancellationSource.Token.Register(() => SetCanceled(), false);
            _TaskSource.Task.ConfigureAwait(false);
        }

        public WorkItem(
            Action doWork
            , TaskCreationOptions options = TaskCreationOptions.None
            , int secsBeforeCanceling = 5)
        {
            if (doWork is null) throw new ArgumentNullException(nameof(doWork));
            if (secsBeforeCanceling <= 0) throw new ArgumentNullException(nameof(secsBeforeCanceling));

            _SecsBeforeCancellingMe = secsBeforeCanceling * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI;
            _CancellationSource = new CancellationTokenSource(_SecsBeforeCancellingMe);
            _TaskSource = new TaskCompletionSource<object>(options);
            _DoWork = doWork;
            _Disposed = false;

            _CancellationSource.Token.Register(() => SetCanceled(), false);
            _TaskSource.Task.ConfigureAwait(false);
        }

        public void SetCanceled()
        {
            _TaskSource.TrySetCanceled();
        }

        public void SetCompletition(object result)
        {
            _TaskSource.TrySetResult(result);
        }

        public void SetException(Exception ex)
        {
            _TaskSource.TrySetException(ex);
        }

        public virtual void Dispose(bool disposing)
        {
            if (_Disposed) return;

            if (disposing)
            {
                _CancellationSource.Dispose();
                _TaskSource.Task.Dispose();
            }
        }
    }
}
