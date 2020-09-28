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
            , int secsBeforeCanceling = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS)
        {
            if (doWork is null) throw new ArgumentNullException(nameof(doWork));
            if (secsBeforeCanceling <= 0) throw new ArgumentOutOfRangeException(nameof(secsBeforeCanceling));
            if (doWork.Method.ReturnType is TData is false) throw new InvalidCastException(nameof(doWork));

            var item = new WorkItem<TData>(doWork, creationOptions, secsBeforeCanceling);
            _WorkItemsQueue.Add(item);
        }

        public void EnqueueWork(
            Func<TData> doWork
            , Action<TData> doWorkCallback = null
            , TaskCreationOptions creationOptions = TaskCreationOptions.None
            , int secsBeforeCanceling = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS)
        {
            if (doWork is null) throw new ArgumentNullException(nameof(doWork));
            if (secsBeforeCanceling <= 0) throw new ArgumentOutOfRangeException(nameof(secsBeforeCanceling));
            if (doWork.Method.ReturnType is TData is false) throw new InvalidCastException(nameof(doWork));

            var item = new WorkItem<TData>(doWork, creationOptions, secsBeforeCanceling);
            _WorkItemsQueue.Add(item);
        }

        public bool TryExecuteWorkNow(
             Func<TData> doWork
            , TaskCreationOptions creationOptions = TaskCreationOptions.None
            , int secsBeforeCanceling = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS) => TryExecuteWorkNow(doWork, null, creationOptions, secsBeforeCanceling);

        public bool TryExecuteWorkNow(
            Func<TData> doWork
            , Action<TData> doWorkCallback = null
            , TaskCreationOptions creationOptions = TaskCreationOptions.None
            , int secsBeforeCanceling = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS)
        {
            var item = doWorkCallback is null
               ? new WorkItem<TData>(doWork)
               : new WorkItem<TData>(() => { var res = doWork(); doWorkCallback(res); return res; });

            item.DoWork();

            return this.TryExecuteTask(item.Task);
        }
    }

    public class TPLTaskScheduler : TaskScheduler, ITaskScheduler
    {
        protected readonly BlockingCollection<IWorkItem> _WorkItemsQueue;
        protected readonly CancellationTokenSource _CancelConsumerSource;
        protected readonly int _WaitUntilCancelaWorkItem;
        protected readonly int _ThreadsCount;
        protected readonly ILogger _Logger;

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

        /// <summary>
        /// A Method to setting up the blocking collection alog with its consumer threads
        /// that will dequeue the workItems
        /// </summary>
        /// <param name="consTaskList"></param>
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

        public void EnqueueWork(
            Action doWork
            , TaskCreationOptions creationOptions = TaskCreationOptions.None
            , int secsBeforeCanceling = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS) => EnqueueWork(doWork, null, creationOptions, secsBeforeCanceling);

        public void EnqueueWork(
            Action doWork
            , Action doWorkCallback = null
            , TaskCreationOptions creationOptions = TaskCreationOptions.None
            , int secsBeforeCanceling = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS)
        {
            if (_WorkItemsQueue.IsCompleted || _WorkItemsQueue.IsAddingCompleted) return;

            var workItem = doWorkCallback is null
                ? new WorkItem(doWork)
                : new WorkItem(() => { doWork(); doWorkCallback(); });

            _WorkItemsQueue.Add(workItem);

            Debug.WriteLine($"Enqueueing the new WorkItem {workItem.Id}");
        }

        public bool TryExecuteWorkNow(
            Action doWork
            , TaskCreationOptions creationOptions = TaskCreationOptions.None
            , int secsBeforeCanceling = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS) => TryExecuteWorkNow(doWork, null);

        public bool TryExecuteWorkNow(
            Action doWork
            , Action doWorkCallback = null
            , TaskCreationOptions creationOptions = TaskCreationOptions.None
            , int secsBeforeCanceling = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS)
        {
            var workItem = doWorkCallback is null
                ? new WorkItem(doWork)
                : new WorkItem(() => { doWork(); doWorkCallback(); });

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

        /// <summary>
        /// Validate if the task is still runnable and valid
        /// </summary>
        /// <param name="task">The task to be validated</param>
        /// <returns></returns>
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

        /// <summary>
        /// A Try-catch wrapper used to run 'safetly' the task and get logs in case of failure
        /// </summary>
        /// <param name="task"></param>
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

        /// <summary>
        /// A Try-catch wrapper used to run 'safetly' the WorkItem and get logs in case of failure
        /// </summary>
        /// <param name="task"></param>
        protected virtual bool TryCatchWorkItemWrapper(IWorkItem workItem)
        {
            var result = true;
            try
            {
                workItem.DoWork();
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
                result = false;
            }
            catch (Exception ex)
            {
                workItem.SetException(ex);
                //TODO: log in  log table

                Debug.WriteLine($"OnGeneric exception for WorkItem {workItem.Id}");
                result = false;
            }
            finally
            {
                workItem.Dispose();
                GC.Collect();
            }

            return result;
        }
    }
}
