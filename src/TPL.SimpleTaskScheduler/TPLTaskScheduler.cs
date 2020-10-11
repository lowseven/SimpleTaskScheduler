using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TPL.Interfaces;

namespace TPL.SimpleTaskScheduler
{
    public class TPLTaskScheduler<TData> : TPLTaskScheduler, ITaskScheduler<TData> where TData : class
    {
        public override int MaximumConcurrencyLevel => _ThreadsCount;

        public TPLTaskScheduler(
              int threadsCount = TPLConstants.TPL_SCHEDULER_MIN_THREAD_COUNT
            , int waitUntilCancelWorkItem = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS)
            : base(threadsCount, waitUntilCancelWorkItem)
        {

        }

        public void EnqueueWork(
            Func<TData> doWork
            , TaskCreationOptions creationOptions = TaskCreationOptions.None
            , int dueTime = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS)
        {
            if (doWork is null) throw new ArgumentNullException(nameof(doWork));
            if (dueTime <= 0) throw new ArgumentOutOfRangeException(nameof(dueTime));
            if (doWork.Method.ReturnType is TData is false) throw new InvalidCastException(nameof(doWork));

            var item = new WorkItem<TData>(doWork, creationOptions, dueTime);
            _WorkItemsQueue.Add(item);
        }

        public void EnqueueWork(
            Func<TData> doWork
            , Action<TData> doWorkCallback = null
            , TaskCreationOptions creationOptions = TaskCreationOptions.None
            , int dueTime = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS)
        {
            if (doWork is null) throw new ArgumentNullException(nameof(doWork));
            if (dueTime <= 0) throw new ArgumentOutOfRangeException(nameof(dueTime));
            if (doWork.Method.ReturnType is TData is false) throw new InvalidCastException(nameof(doWork));

            var item = new WorkItem<TData>(doWork, creationOptions, dueTime);
            _WorkItemsQueue.Add(item);
        }

        public bool TryExecuteWorkNow(
             Func<TData> doWork
            , TaskCreationOptions creationOptions = TaskCreationOptions.None
            , int dueTime = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS) => TryExecuteWorkNow(doWork, null, creationOptions, dueTime);

        public bool TryExecuteWorkNow(
            Func<TData> doWork
            , Action<TData> doWorkCallback = null
            , TaskCreationOptions creationOptions = TaskCreationOptions.None
            , int dueTime = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS)
        {
            if (doWork is null) throw new ArgumentNullException(nameof(doWork));
            if (dueTime <= 0) throw new ArgumentOutOfRangeException(nameof(dueTime));
            if (doWork.Method.ReturnType is TData is false) throw new InvalidCastException(nameof(doWork));

            var item = doWorkCallback is null
               ? new WorkItem<TData>(doWork)
               : new WorkItem<TData>(() => { var res = doWork(); doWorkCallback(res); return res; });

            item.DoWork();

            return this.TryExecuteTask(item.Task);
        }
    }

    public class TPLTaskScheduler : TaskScheduler, ITaskScheduler
    {
        protected readonly static object _lock = new object();

        protected readonly BlockingCollection<IWorkItem> _WorkItemsQueue;
        protected readonly int _MaximumQueueItems;
        protected readonly int _TaskDueTime;
        protected readonly int _ThreadsCount;
        protected readonly ILogger _Logger;

        protected IEnumerable<Tuple<Task, CancellationTokenSource>> _ConsumerTaskList;
        protected bool _Disposed;

        public bool AllTasksCompleted => GetScheduledTasks().Any() is false;
        public override int MaximumConcurrencyLevel => _ThreadsCount;

        public IEnumerable<IWorkItem> EnqueuedWorkItems => _WorkItemsQueue.AsEnumerable();

        public TPLTaskScheduler(
              int threadsCount = TPLConstants.TPL_SCHEDULER_MIN_THREAD_COUNT
            , int taskDueTime = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS
            , int maxQueueItems = TPLConstants.TPL_SCHEDULER_MAX_QUEUE_ITEMS)
        {
            if (taskDueTime <= 0) throw new ArgumentOutOfRangeException(nameof(taskDueTime));
            if (threadsCount <= 0) throw new ArgumentOutOfRangeException(nameof(threadsCount));

            _Disposed = false;
            _TaskDueTime = taskDueTime;
            _MaximumQueueItems = maxQueueItems;
            _WorkItemsQueue = new BlockingCollection<IWorkItem>();
            _ThreadsCount = threadsCount;
            _Logger = TPLUtils.GetLogger();

            StartingUpConsumerThreads();
        }

        /// <summary>
        /// A Method to setting up the blocking collection alog with its consumer threads
        /// that will dequeue the workItems
        /// </summary>
        private void StartingUpConsumerThreads()
        {
            var ctSource = new CancellationTokenSource();
            var list = new List<Tuple<Task, CancellationTokenSource>>(_ThreadsCount);

            for (int i = 0; i < _ThreadsCount; i++)
            {
                var task = new Task(() => HandleConsumeCollection(ctSource.Token), ctSource.Token);
                task.ConfigureAwait(false);
                task.ContinueWith((e) =>
                {
                    if (IsValidTask(e) is false)
                    {
                        _Logger.Debug($"{e.Exception}");
                    }
                    else
                    {
                        _Logger.Debug($"Scheduler Consumer Task {e.Id} DONE");
                    }
                });

                list.Add(Tuple.Create(task, ctSource));
                ctSource.Token.ThrowIfCancellationRequested();

                task.Start();

                ctSource.Token.ThrowIfCancellationRequested();
                _Logger.Debug($"Starting consumer Thread {task.Id}");
            }

            _ConsumerTaskList = list;
            ctSource.Token.Register(() =>
            {
                if (_Disposed is false)
                {
                    _Logger.Debug($"Scheduler Consumer Tasks CANCELED");
                    ctSource.Dispose();
                }

            });
        }

        public void Dispose() => Dispose(true);
        protected void Dispose(bool disposing)
        {
            if (_Disposed) return;

            if (disposing)
            {
                foreach (var cts in _ConsumerTaskList)
                {
                    if (cts.Item2.IsCancellationRequested is false)
                    {
                        cts.Item2.Cancel();
                    }
                }

                _WorkItemsQueue.CompleteAdding();
                _Disposed = true;
            }
        }

        public void EnqueueWork(
            Action doWork
            , TaskCreationOptions creationOptions = TaskCreationOptions.None
            , int dueTime = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS) => EnqueueWork(doWork, null, creationOptions, dueTime);

        public void EnqueueWork(
            Action doWork
            , Action doWorkCallback
            , TaskCreationOptions creationOptions = TaskCreationOptions.None
            , int dueTime = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS)
        {
            if (CanAddWorkItem())
                throw new InvalidOperationException(string.Format(TPLConstants.TPL_SCHEDULER_MAX_QUEUE_ITEMS_EX, this._MaximumQueueItems));

            if (doWork is null) throw new ArgumentNullException(nameof(doWork));
            if (dueTime <= 0) throw new ArgumentOutOfRangeException(nameof(dueTime));

            if (_WorkItemsQueue.IsCompleted || _WorkItemsQueue.IsAddingCompleted)
                throw new ObjectDisposedException("Object Disposed Cannot add more workitem");

            var workItem = doWorkCallback is null
                ? new WorkItem(doWork, creationOptions, dueTime)
                : new WorkItem(() => { doWork(); doWorkCallback(); }, creationOptions, dueTime);

            _WorkItemsQueue.Add(workItem);
            _Logger.Debug($"Enqueueing a WorkItem {workItem.Id}");
        }

        public void EnqueueWork(IWorkItem work)
        {
            if (CanAddWorkItem())
                throw new InvalidOperationException(string.Format(TPLConstants.TPL_SCHEDULER_MAX_QUEUE_ITEMS_EX, this._MaximumQueueItems));

            if (work is null) throw new ArgumentNullException(nameof(work));

            if (_WorkItemsQueue.IsCompleted || _WorkItemsQueue.IsAddingCompleted)
                throw new ObjectDisposedException("Object Disposed Cannot add more workitem");

            _WorkItemsQueue.Add(work);
            _Logger.Debug($"Enqueueing the new WorkItem {work.Id}");
        }

        public void EnqueueWork(IEnumerable<IWorkItem> works)
        {
            if (CanAddWorkItem())
                throw new InvalidOperationException(string.Format(TPLConstants.TPL_SCHEDULER_MAX_QUEUE_ITEMS_EX, this._MaximumQueueItems));

            if (works is null) throw new ArgumentNullException(nameof(works));
            if (works.Any() is false) throw new ArgumentOutOfRangeException(nameof(works));

            if (_WorkItemsQueue.IsCompleted || _WorkItemsQueue.IsAddingCompleted)
                throw new ObjectDisposedException("Object Disposed Cannot add more workitem");

            foreach (var work in works)
            {
                _WorkItemsQueue.Add(work);
                _Logger.Debug($"Enqueueing the new WorkItem {work.Id}");
            }
        }

        public bool TryExecuteItNow(IWorkItem work)
        {
            if (work is null) throw new ArgumentNullException(nameof(work));
            if (work.IsRunnable is false) return false;

            Func<Task> t = async () => await work;
            t();

            return true;
        }

        public bool TryExecuteItNow(
            Action doWork
            , TaskCreationOptions creationOptions = TaskCreationOptions.None
            , int dueTime = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS) => TryExecuteItNow(doWork, null, creationOptions, dueTime);

        public bool TryExecuteItNow(
            Action doWork
            , Action doWorkCallback
            , TaskCreationOptions creationOptions = TaskCreationOptions.None
            , int dueTime = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS)
        {
            if (_Disposed)
                throw new ObjectDisposedException(nameof(TPLTaskScheduler));
            
            if (CanAddWorkItem())
                throw new InvalidOperationException(string.Format(TPLConstants.TPL_SCHEDULER_MAX_QUEUE_ITEMS_EX, this._MaximumQueueItems));

            if (doWork is null) throw new ArgumentNullException(nameof(doWork));
            if (dueTime < 1) throw new ArgumentOutOfRangeException(nameof(dueTime));

            var item = doWorkCallback is null
                ? new WorkItem(doWork, creationOptions, dueTime)
                : new WorkItem(() => { doWork(); doWorkCallback(); }, creationOptions, dueTime);

            return TryCatchWorkItemWrapper(item);
        }

        protected bool CanAddWorkItem()
        {
            lock (_lock)
            {
                return _MaximumQueueItems > 0 && GetScheduledTasks().Count() <= _MaximumQueueItems;
            }
        }

        /// <summary>
        /// Handle the dequeue assigned to a task consumer
        /// </summary>
        /// <param name="cancellation">The consumer cancellation token</param>
        private void HandleConsumeCollection(CancellationToken cancellation)
        {
            // This sequence that we’re enumerating will block when no elements
            // are available and will end when CompleteAdding is called. 
            foreach (var workItem in _WorkItemsQueue.GetConsumingEnumerable())
            {
                try
                {
                    cancellation.ThrowIfCancellationRequested();

                    if (workItem.IsValid is false)
                    {
                        workItem.SetCanceled();

                        _Logger.Debug($"WorkItemCanceled ID {workItem.Id}");
                    }
                    else
                    {
                        TryCatchWorkItemWrapper(workItem);
                    }

                    cancellation.ThrowIfCancellationRequested();
                }
                catch (OperationCanceledException ex)
                {
                    _Logger.Fatal($"Consumer Task canceled on WorkItem ID {workItem.Id} - {ex.Message}");

                }
                catch (Exception ex)
                {
                    _Logger.Fatal($"Consumer Task canceled on WorkItem ID {workItem.Id} - { ex.Message }");
                }
            }
        }

        protected override void QueueTask(Task task)
        {
            if (CanAddWorkItem())
                throw new InvalidOperationException(string.Format(TPLConstants.TPL_SCHEDULER_MAX_QUEUE_ITEMS_EX, this._MaximumQueueItems));

            var work = new WorkItem(() =>
            {
                var res = task;

                res.ConfigureAwait(false);
                res.Wait();
            }
            , task.CreationOptions
            , _TaskDueTime);

            this._WorkItemsQueue.Add(work);
        }

        /// <summary>
        /// Determines whether the provided System.Threading.Tasks.Task can be executed synchronously
        /// in this call, and if it can, executes it.
        /// NOTE: This task will be run by the default dotnet scheduler by calling TryExecuteTask() method.
        /// </summary>
        /// <param name="task"></param>
        /// <param name="taskWasPreviouslyQueued"></param>
        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            if (IsValidTask(task) is false) return false;

            if (taskWasPreviouslyQueued)
            {
                var workItem = _WorkItemsQueue.SingleOrDefault(i => i.Task.Equals(task));
                if (workItem is null is false)
                {
                    if (workItem.IsValid is false) return false;
                    return TryCatchWorkItemWrapper(workItem);
                }
                else
                {
                    //it was called by using this scheduler
                    return TryCatchTaskWrapper(task);
                }
            }
            else
            {
                return TryCatchTaskWrapper(task);
            }
        }

        /// <summary>
        /// Validate if the task is still runnable and valid
        /// </summary>
        /// <param name="task">The task to be validated</param>
        protected virtual bool IsValidTask(Task task) =>
                   task is null is false
                && task.IsCanceled is false
                && task.IsCompleted is false
                && task.IsFaulted is false
                && task.IsCompletedSuccessfully is false
                && task.Exception is null;

        protected override IEnumerable<Task> GetScheduledTasks()
        {
            //taking a snapshot
            var list = _WorkItemsQueue.ToList();

            return list.Select(i => i.Task);
        }

        /// <summary>
        /// A Try-catch wrapper used to run 'safetly' the task and get logs in case of failure
        /// </summary>
        /// <param name="task">The task to be executed</param>
        private bool TryCatchTaskWrapper(Task task)
        {
            var result = false;
            try
            {
                task.ConfigureAwait(false);
                TryExecuteTask(task);
                result = true;
            }
            catch (InvalidOperationException ex)
            {
                _Logger.Debug($"OnCancellationRequest exception for WorkItem {task.Id} {task.Exception} {ex.Message}");
            }
            catch (OperationCanceledException ex)
            {
                //a cancellation token was send in the middle of the job/work
                if (ex.CancellationToken.IsCancellationRequested)
                {
                    _Logger.Debug($"OnCancellationRequest exception for WorkItem {task.Id} {task.Exception} {ex.Message}");
                }
                else
                {
                    _Logger.Debug($"OnOperationCanceledException exception for WorkItem {task.Id} {task.Exception} {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                _Logger.Debug($"OnGeneric exception for WorkItem {task.Id} {task.Exception} - {ex.Message}");
            }
            finally
            {
                task.Dispose();
            }

            return result;
        }

        /// <summary>
        /// A Try-catch wrapper used to run 'safetly' the WorkItem and get logs in case of failure
        /// </summary>
        /// <param name="task"></param>
        private bool TryCatchWorkItemWrapper(IWorkItem workItem)
        {
            var result = false;
            try
            {
                workItem.DoWork();
                workItem.SetResult();
                result = true;
            }
            catch (OperationCanceledException ex)
            {
                //a cancellation token was send in the middle of the job/work
                if (ex.CancellationToken.IsCancellationRequested)
                {
                    workItem.SetCanceled();
                    _Logger.Debug($"OnCancellationRequest exception for WorkItem {workItem.Id}");
                }
                else
                {
                    workItem.SetException(ex);
                    _Logger.Debug($"OnOperationCanceledException exception for WorkItem {workItem.Id}");
                }
            }
            catch (Exception ex)
            {
                workItem.SetException(ex);

                _Logger.Debug($"OnGeneric exception for WorkItem {workItem.Id}");
            }

            return result;
        }
    }
}
