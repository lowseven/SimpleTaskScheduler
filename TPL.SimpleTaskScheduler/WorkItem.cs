using System;
using System.Threading;
using System.Threading.Tasks;
using TPL.Interfaces;

namespace TPL.SimpleTaskScheduler
{
    /// <summary>
    /// Represents the a Work that will be processed
    /// [by default the canceling time is 5 seconds]
    /// </summary>
    public class WorkItem<TData> : IWorkItem<TData> where TData : class
    {
        public int Id { get => _TaskSource.Task.Id; }
        public bool IsValid { get => _CancellationSource.IsCancellationRequested || _TaskSource.Task.IsCanceled || _TaskSource.Task.IsFaulted; }

        public Task<TData> Task => _TaskSource.Task;
        public Func<TData> DoWork => _DoWork;
        public Action<TData, object> DoWorkCallback => _DoWorkCallback;
        public void Dispose() => Dispose(true);
  

        private bool _Disposed;
        private Func<TData> _DoWork;
        private Action<TData, object> _DoWorkCallback;
        private readonly int _SecsBeforeCancellingMe;
        private readonly CancellationTokenSource _CancellationSource;
        private readonly TaskCompletionSource<TData> _TaskSource;

        public WorkItem(
            Func<TData> doWork
            , Action<TData, object> doWorkCallback = null
            , TaskCreationOptions options = TaskCreationOptions.None
            , int secsBeforeCanceling = 5)
        {
            if (doWork is null) throw new ArgumentNullException(nameof(doWork));
            if (secsBeforeCanceling <= 0) throw new ArgumentNullException(nameof(secsBeforeCanceling));

            _SecsBeforeCancellingMe = secsBeforeCanceling * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI;
            _CancellationSource = new CancellationTokenSource(_SecsBeforeCancellingMe);
            _TaskSource = new TaskCompletionSource<TData>(options);
            _DoWork = doWork;
            _DoWorkCallback = doWorkCallback;
            _Disposed = false;

            _CancellationSource.Token.Register(() => SetCanceled(), false);

            _TaskSource.Task.ConfigureAwait(false);
        }

        public void SetCanceled()
        {
            _TaskSource.TrySetCanceled();
        }

        public void SetResult(TData result)
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
    public class WorkItem : IWorkItem
    {
        public int Id { get => _TaskSource.Task.Id; }
        public bool IsValid { get => _CancellationSource.IsCancellationRequested || _TaskSource.Task.IsCanceled || _TaskSource.Task.IsFaulted; }

        public Task Task => _TaskSource.Task;
        public Action DoWork => _DoWork;
        public Action<object> DoWorkCallback => _DoWorkCallback;
        public void Dispose() => Dispose(true);
        //TBD
        //sent intent cancellation from the scheduler
        internal void CancelWork() => _CancellationSource.Cancel(true);

        private bool _Disposed;
        private Action _DoWork;
        private Action<object> _DoWorkCallback;
        private readonly int _SecsBeforeCancellingMe;
        private readonly CancellationTokenSource _CancellationSource;
        private readonly TaskCompletionSource<object> _TaskSource;

        public WorkItem(
            Action doWork
            , Action<object> doWorkCallback = null
            , TaskCreationOptions options = TaskCreationOptions.None
            , int secsBeforeCanceling = 5)
        {
            if (doWork is null) throw new ArgumentNullException(nameof(doWork));
            if (secsBeforeCanceling <= 0) throw new ArgumentNullException(nameof(secsBeforeCanceling));

            _SecsBeforeCancellingMe = secsBeforeCanceling * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI;
            _CancellationSource = new CancellationTokenSource(_SecsBeforeCancellingMe);
            _TaskSource = new TaskCompletionSource<object>(options);
            _DoWork = doWork;
            _DoWorkCallback = doWorkCallback;
            _Disposed = false;

            _CancellationSource.Token.Register(() => SetCanceled(), false);

            _TaskSource.Task.ConfigureAwait(false);
        }

        public void SetCanceled()
        {
            _TaskSource.TrySetCanceled();
        }

        public void SetCompletition()
        {
            _TaskSource.TrySetResult(null);
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
