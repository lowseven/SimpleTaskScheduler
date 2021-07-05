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
    public class WorkItem<TData> : WorkItem, IWorkItem<TData>
    {
        public WorkItem(
            Func<TData> doWork
            , TaskCreationOptions options = TaskCreationOptions.None
            , int dueTime = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS) : base(options, dueTime)
        {
            if (doWork is null) throw new ArgumentNullException(nameof(doWork));
            if (dueTime < 1) throw new ArgumentOutOfRangeException(nameof(dueTime));

            _WorkItemResult = null;
            _DoWork = () => { _WorkItemResult = doWork(); };
            _Disposed = false;
        }

        public new TData GetResult()
        {
            ThrowIfInvalid();

            return (TData) this._WorkItemResult ;
        }

        public new IWorkItem<TData> GetAwaiter()
        {
            base.GetAwaiter();

            return this;
        }

        public TData Result => (TData)_WorkItemResult;
    }

    /// <summary>
    /// Represents the a Work that will be processed,
    /// by default the canceling time is 5 seconds
    /// </summary>
    public class WorkItem : IWorkItem
    {
        public int Id { get => _TaskSource.Task.Id; }
        public bool IsValid
        {
            get => _Disposed is false
                && _CancellationSource.IsCancellationRequested is false
                && _TaskSource.Task.IsCanceled is false
                && _TaskSource.Task.IsFaulted is false
                && IsCompleted is false;
        }

        public bool IsRunnable
        {
            get =>
                IsValid
                && _TaskSource.Task.IsCompleted is false;
        }

        public bool IsCompleted =>
            IsCanceled is false
            && _TaskSource.Task.IsCompleted;

        public bool IsCanceled => _TaskSource.Task.IsCanceled && _CancellationSource.IsCancellationRequested;
        public Task Task => _TaskSource.Task;
        public Action DoWork => _DoWork;

        protected bool _Disposed;
        protected object _WorkItemResult;
        protected Action _DoWork;
        protected readonly int _DueTime;
        protected readonly CancellationTokenSource _CancellationSource;
        protected readonly TaskCompletionSource<object> _TaskSource;

        internal WorkItem(
             TaskCreationOptions options = TaskCreationOptions.None
           , int dueTime = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS)
        {
            if (dueTime <= 0) throw new ArgumentOutOfRangeException(nameof(dueTime));

            _DueTime = dueTime * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI;
            _CancellationSource = new CancellationTokenSource(_DueTime);
            _TaskSource = new TaskCompletionSource<object>(options);
            _TaskSource.Task.ConfigureAwait(false);
            _Disposed = false;
            _CancellationSource.Token.Register(() =>
            {
                if (_TaskSource.Task.IsFaulted
                    || _TaskSource.Task.IsCompleted
                    || _TaskSource.Task.IsCanceled) return;

                _TaskSource.SetCanceled();
            });
        }

        public WorkItem(
            Action doWork
            , TaskCreationOptions options = TaskCreationOptions.None
            , int dueTime = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS)
        {
            if (doWork is null) throw new ArgumentNullException(nameof(doWork));
            if (dueTime < 1) throw new ArgumentOutOfRangeException(nameof(dueTime));

            _DueTime = dueTime * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI;
            _CancellationSource = new CancellationTokenSource();
            _TaskSource = new TaskCompletionSource<object>(options);
            _DoWork = doWork;
            _Disposed = false;
            _CancellationSource.CancelAfter(_DueTime);
            _TaskSource.Task.ConfigureAwait(false);
            _CancellationSource.Token.Register(() =>
            {
                if (_TaskSource.Task.IsFaulted
                    || _TaskSource.Task.IsCompleted
                    || _TaskSource.Task.IsCanceled) return;

                _TaskSource.SetCanceled();
            });
        }

        public void SetCanceled()
        {
            if (_Disposed) throw new ObjectDisposedException(nameof(WorkItem));
            if (this.IsCompleted) throw new InvalidOperationException("The Work Item is already completed");

            NotifyCancellation();
        }

        public void SetResult()
        {
            _TaskSource.SetResult(this._WorkItemResult);
        }

        public void SetException(Exception ex)
        {
            ThrowIfInvalid();

            _TaskSource.SetException(ex);
        }

        public void OnCompleted(Action continuation)
        {
            ThrowIfInvalid();

            if (continuation is null) return;

            _TaskSource.Task
                .ContinueWith((t) => continuation())
                .Wait();
        }

        public void GetResult()
        {
            ThrowIfInvalid();
        }

        public IWorkItem GetAwaiter()
        {
            ThrowIfInvalid();

            _DoWork();

            if (IsValid)
            {
                _TaskSource.SetResult(_WorkItemResult);
            }
            else
            {
                _TaskSource.SetCanceled();
            }

            return this;
        }

        protected virtual void ThrowIfInvalid()
        {
            if (_Disposed) throw new ObjectDisposedException(nameof(WorkItem));

            _CancellationSource.Token.ThrowIfCancellationRequested();
        }

        public void Dispose() => Dispose(true);
        private void Dispose(bool disposing)
        {
            if (_Disposed) return;

            if (disposing)
            {
                if (_TaskSource.Task.IsCompleted is false
                    || _TaskSource.Task.IsFaulted is false
                    || _TaskSource.Task.IsCanceled is false)
                {
                    _TaskSource.SetCanceled();
                }

                _CancellationSource.Dispose();
                _TaskSource.Task.Dispose();
                _Disposed = true;
            }
        }

        public void NotifyCancellation()
        {
            if (_Disposed) throw new ObjectDisposedException(nameof(WorkItem));
            if (_CancellationSource.Token.CanBeCanceled is false) return;

            _CancellationSource.Cancel();
        }
    }
}
