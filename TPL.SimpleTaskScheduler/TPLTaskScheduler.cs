using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TPL.Interfaces;
using TPL.SimpleTaskScheduler.Interfaces;

namespace TPL.SimpleTaskScheduler
{
    public class TPLTaskScheduler<TData> : TaskScheduler, ITaskScheduler<TData> where TData : class
    {
        private readonly BlockingCollection<IWorkItem<TData>> _WorkItemsQueue;
        private readonly CancellationTokenSource _CancelConsumerSource;
        private readonly int _WaitUntilCancelaWorkItem;
        private readonly int _ThreadsCount;

        private readonly ILogger _Logger;

        public override int MaximumConcurrencyLevel => _ThreadsCount;
        public bool AllTasksCompleted => GetScheduledTasks().Any();

        public TPLTaskScheduler(
              int threadsCount = TPLConstants.TPL_SCHEDULER_MIN_THREAD_COUNT
            , int waitUntilCancelWorkItem = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS)
        {
            var consTaskList = new List<Task>(threadsCount);

            _WaitUntilCancelaWorkItem = waitUntilCancelWorkItem;
            _WorkItemsQueue = new BlockingCollection<IWorkItem<TData>>();
            _ThreadsCount = threadsCount;
            _CancelConsumerSource = new CancellationTokenSource();
            _Logger = TPLUtils.GetLogger();

            StartingUpThreads(consTaskList);

            _Logger.Debug($"Starting up the scheduler with {consTaskList.Count} consumers");
        }

        protected virtual void StartingUpThreads(ICollection<Task> consTaskList)
        {
            for (int i = 0; i < _ThreadsCount; i++)
            {
                var task = new Task(HandleConsumeCollection, _CancelConsumerSource.Token, TaskCreationOptions.LongRunning);
                task.ConfigureAwait(false);
                task.ContinueWith((e) =>
                {
                    if (IsValidTask(e) is false)
                    {
                        //TODO: log here something went wrong
                        Debug.WriteLine($"{e.Exception}");
                    }
                    else
                    {
                        Debug.WriteLine($"\nScheduler Thread {e.Id} DONE");
                    }
                });

                task.Start();
                consTaskList.Add(task);
            }
        }

        public void Dispose()
        {
            if (_CancelConsumerSource.IsCancellationRequested is false)
            {
                _CancelConsumerSource.Cancel();
            }

            _CancelConsumerSource.Dispose();
            _WorkItemsQueue.CompleteAdding();
        }

        public void EnqueueWork(Func<TData> doWork) => EnqueueWork(doWork, null);

        public void EnqueueWork(Func<TData> doWork, Action<TData, object> doWorkCallback = null)
        {
            if (_WorkItemsQueue.IsCompleted || _WorkItemsQueue.IsAddingCompleted) return;

            var workItem = new WorkItem<TData>(
                doWork
                , doWorkCallback
                , TaskCreationOptions.PreferFairness
                , _WaitUntilCancelaWorkItem);

            _WorkItemsQueue.Add(workItem);

            Debug.WriteLine($"Enqueueing the new WorkItem {workItem.Id}");
        }

        public bool TryExecuteWorkNow(Func<TData> doWork) => TryExecuteWorkNow(doWork, null);

        public bool TryExecuteWorkNow(Func<TData> doWork, Action<TData, object> doWorkCallback = null)
        {
            var workItem = new WorkItem<TData>(doWork, doWorkCallback);

            return TryExecuteTaskInline(workItem.Task, false);
        }

        private void HandleConsumeCollection()
        {
            // This sequence that we’re enumerating will block when no elements
            // are available and will end when CompleteAdding is called.
            foreach (var workItem in _WorkItemsQueue.GetConsumingEnumerable())
            {
                if (workItem.IsValid is false)
                {
                    workItem.SetCanceled();

                    Debug.WriteLine($"WorkItemCanceled ID {workItem.Id}");
                }
                else
                {
                    TryCatchWorkItemWrapper(workItem);
                }
            }
        }

        protected override void QueueTask(Task task)
        {
            var work = new WorkItem<TData>(() =>
            {
                var res = (Task<TData>)task;
                res.ConfigureAwait(false);

                return res.Result;
            }
            , null
            , task.CreationOptions
            , _WaitUntilCancelaWorkItem);

            this._WorkItemsQueue.Add(work);
        }

        /// <summary>
        /// Determines whether the provided System.Threading.Tasks.Task can be executed synchronously
        /// in this call, and if it can, executes it.
        /// NOTE: This task will be run by the default dotnet scheduler by calling TryExecuteTask() method.
        /// </summary>
        /// <param name="task"></param>
        /// <param name="taskWasPreviouslyQueued"></param>
        /// <returns></returns>
        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            if (IsValidTask(task) is false) return false;

            if (taskWasPreviouslyQueued is false)
            {
                TryCatchTaskWrapper(task);
            }
            else
            {
                var workItem = _WorkItemsQueue.SingleOrDefault(i => i.Id.Equals(task.Id));
                if (workItem is null || workItem.IsValid is false) return false;

                TryCatchWorkItemWrapper(workItem);
            }

            return true;
        }

        protected virtual bool IsValidTask(Task task) =>
            task is null is false && (
                task.IsCanceled is false
                || task.IsCompleted is false
                || task.IsFaulted is false
                || task.IsCompletedSuccessfully is false
                || task.Exception is null);

        protected override IEnumerable<Task> GetScheduledTasks()
        {
            //taking a snapshot
            var list = _WorkItemsQueue.ToList();

            return list.Select(i => i.Task);
        }

        protected virtual void TryCatchTaskWrapper(Task task)
        {
            try
            {
                task.ConfigureAwait(false);
                TryExecuteTask(task);
            }
            catch (InvalidOperationException ex)
            {
                Debug.WriteLine($"OnCancellationRequest exception for WorkItem {task.Id} {task.Exception} {ex.Message}");
            }
            catch (OperationCanceledException ex)
            {
                //a cancellation token was send in the middle of the job/work
                if (ex.CancellationToken.IsCancellationRequested)
                {
                    //workItem.SetCanceled();
                    //TODO: log in  log table
                    Debug.WriteLine($"OnCancellationRequest exception for WorkItem {task.Id} {task.Exception} {ex.Message}");

                }
                else
                {
                    //workItem.SetException(ex);
                    //TODO: log in  log table
                    Debug.WriteLine($"OnOperationCanceledException exception for WorkItem {task.Id} {task.Exception} {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                //workItem.SetException(ex);
                //TODO: log in  log table

                Debug.WriteLine($"OnGeneric exception for WorkItem {task.Id} {task.Exception}");
            }
            finally
            {
                task.Dispose();
            }
        }

        protected virtual void TryCatchWorkItemWrapper(IWorkItem<TData> workItem)
        {
            try
            {
                var result = workItem.DoWork();
                workItem.SetResult(result);

                if (workItem.DoWorkCallback is null is false)
                {
                    workItem.DoWorkCallback(result, workItem.Task.AsyncState);
                }

                result = null;
                GC.Collect();
            }
            catch (OperationCanceledException ex)
            {
                //a cancellation token was send in the middle of the job/work
                if (ex.CancellationToken.IsCancellationRequested)
                {
                    workItem.SetCanceled();
                    //TODO: log in  log table
                    Debug.WriteLine($"OnCancellationRequest exception for WorkItem {workItem.Id}");

                }
                else
                {
                    workItem.SetException(ex);
                    //TODO: log in  log table
                    Debug.WriteLine($"OnOperationCanceledException exception for WorkItem {workItem.Id}");
                }
            }
            catch (Exception ex)
            {
                workItem.SetException(ex);
                //TODO: log in  log table

                Debug.WriteLine($"OnGeneric exception for WorkItem {workItem.Id}");
            }
            finally
            {
                workItem.Dispose();
            }
        }
    }

    public class TPLTaskScheduler : TaskScheduler, ITaskScheduler
    {
        private readonly BlockingCollection<IWorkItem> _WorkItemsQueue;
        private readonly CancellationTokenSource _CancelConsumerSource;
        private readonly int _WaitUntilCancelaWorkItem;
        private readonly int _ThreadsCount;

        public override int MaximumConcurrencyLevel => _ThreadsCount;
        public bool AllTasksCompleted => GetScheduledTasks().Any();

        public TPLTaskScheduler(
              int threadsCount = TPLConstants.TPL_SCHEDULER_MIN_THREAD_COUNT
            , int waitUntilCancelWorkItem = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS)
        {
            var consTaskList = new List<Task>(threadsCount);

            _WaitUntilCancelaWorkItem = waitUntilCancelWorkItem;
            _WorkItemsQueue = new BlockingCollection<IWorkItem>();
            _ThreadsCount = threadsCount;
            _CancelConsumerSource = new CancellationTokenSource();

            StartingUpThreads(consTaskList);
        }

        private void StartingUpThreads(ICollection<Task> consTaskList)
        {
            for (int i = 0; i < _ThreadsCount; i++)
            {
                var task = new Task(HandleConsumeCollection, _CancelConsumerSource.Token, TaskCreationOptions.LongRunning);
                task.ConfigureAwait(false);
                task.ContinueWith((e) =>
                {
                    if (IsValidTask(e) is false)
                    {
                        //TODO: log here something went wrong
                        Debug.WriteLine($"{e.Exception}");
                    }
                    else
                    {
                        Debug.WriteLine($"\nScheduler Thread {e.Id} DONE");
                    }
                });

                task.Start();
                consTaskList.Add(task);
            }
        }

        public void Dispose()
        {
            if (_CancelConsumerSource.IsCancellationRequested is false)
            {
                _CancelConsumerSource.Cancel();
            }

            _CancelConsumerSource.Dispose();
            _WorkItemsQueue.CompleteAdding();
        }

        public void EnqueueWork(Action doWork) => EnqueueWork(doWork, null);

        public void EnqueueWork(Action doWork, Action<object> doWorkCallback = null)
        {
            if (_WorkItemsQueue.IsCompleted || _WorkItemsQueue.IsAddingCompleted) return;

            var workItem = new WorkItem(
                doWork
                , doWorkCallback
                , TaskCreationOptions.PreferFairness
                , _WaitUntilCancelaWorkItem);

            _WorkItemsQueue.Add(workItem);

            Debug.WriteLine($"Enqueueing the new WorkItem {workItem.Id}");
        }

        public bool TryExecuteWorkNow(Action doWork) => TryExecuteWorkNow(doWork, null);

        public bool TryExecuteWorkNow(Action doWork, Action<object> doWorkCallback = null)
        {
            var workItem = new WorkItem(doWork, doWorkCallback);

            return TryExecuteTaskInline(workItem.Task, false);
        }

        protected virtual void HandleConsumeCollection()
        {
            // This sequence that we’re enumerating will block when no elements
            // are available and will end when CompleteAdding is called. 
            foreach (var workItem in _WorkItemsQueue.GetConsumingEnumerable())
            {
                if (workItem.IsValid is false)
                {
                    workItem.SetCanceled();

                    Debug.WriteLine($"WorkItemCanceled ID {workItem.Id}");
                }
                else
                {
                    TryCatchWorkItemWrapper(workItem);
                }
            }
        }

        protected override void QueueTask(Task task)
        {
            var work = new WorkItem(() =>
            {
                var res = task;
                res.ConfigureAwait(false);

                res.Wait();

            }
            , null
            , task.CreationOptions
            , _WaitUntilCancelaWorkItem);

            this._WorkItemsQueue.Add(work);
        }

        /// <summary>
        /// Determines whether the provided System.Threading.Tasks.Task can be executed synchronously
        /// in this call, and if it can, executes it.
        /// NOTE: This task will be run by the default dotnet scheduler by calling TryExecuteTask() method.
        /// </summary>
        /// <param name="task"></param>
        /// <param name="taskWasPreviouslyQueued"></param>
        /// <returns></returns>
        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            if (IsValidTask(task) is false) return false;

            if (taskWasPreviouslyQueued is false)
            {
                TryCatchTaskWrapper(task);
            }
            else
            {
                var workItem = _WorkItemsQueue.SingleOrDefault(i => i.Id.Equals(task.Id));
                if (workItem is null || workItem.IsValid) return false;

                TryCatchWorkItemWrapper(workItem);
            }

            return true;
        }

        protected virtual bool IsValidTask(Task task) =>
            task is null is false && (
                task.IsCanceled is false
                || task.IsCompleted is false
                || task.IsFaulted is false
                || task.IsCompletedSuccessfully is false
                || task.Exception is null);

        protected override IEnumerable<Task> GetScheduledTasks()
        {
            //taking a snapshot
            var list = _WorkItemsQueue.ToList();

            return list.Select(i => i.Task);
        }

        protected virtual void TryCatchTaskWrapper(Task task)
        {
            try
            {
                task.ConfigureAwait(false);
                TryExecuteTask(task);
            }
            catch (InvalidOperationException ex)
            {
                Debug.WriteLine($"OnCancellationRequest exception for WorkItem {task.Id} {task.Exception} {ex.Message}");
            }
            catch (OperationCanceledException ex)
            {
                //a cancellation token was send in the middle of the job/work
                if (ex.CancellationToken.IsCancellationRequested)
                {
                    //workItem.SetCanceled();
                    //TODO: log in  log table
                    Debug.WriteLine($"OnCancellationRequest exception for WorkItem {task.Id} {task.Exception} {ex.Message}");

                }
                else
                {
                    //workItem.SetException(ex);
                    //TODO: log in  log table
                    Debug.WriteLine($"OnOperationCanceledException exception for WorkItem {task.Id} {task.Exception} {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                //workItem.SetException(ex);
                //TODO: log in  log table

                Debug.WriteLine($"OnGeneric exception for WorkItem {task.Id} {task.Exception}");
            }
            finally
            {
                task.Dispose();
            }
        }

        protected virtual void TryCatchWorkItemWrapper(IWorkItem workItem)
        {
            try
            {
                workItem.DoWork();
                workItem.SetCompletition();

                if (workItem.DoWork is null is false)
                {
                    workItem.DoWorkCallback(workItem.Task.AsyncState);
                }

                GC.Collect();
            }
            catch (OperationCanceledException ex)
            {
                //a cancellation token was send in the middle of the job/work
                if (ex.CancellationToken.IsCancellationRequested)
                {
                    workItem.SetCanceled();
                    //TODO: log in  log table
                    Debug.WriteLine($"OnCancellationRequest exception for WorkItem {workItem.Id}");

                }
                else
                {
                    workItem.SetException(ex);
                    //TODO: log in  log table
                    Debug.WriteLine($"OnOperationCanceledException exception for WorkItem {workItem.Id}");
                }
            }
            catch (Exception ex)
            {
                workItem.SetException(ex);
                //TODO: log in  log table

                Debug.WriteLine($"OnGeneric exception for WorkItem {workItem.Id}");
            }
            finally
            {
                workItem.Dispose();
            }
        }
    }
}
