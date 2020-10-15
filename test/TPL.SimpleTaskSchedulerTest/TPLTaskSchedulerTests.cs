using FluentAssertions;
using Moq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TPL.Interfaces;
using TPL.SimpleTaskScheduler;
using TPL.SimpleTaskSchedulerTest.Fakes;
using Xunit;

namespace TPL.SimpleTaskSchedulerTest
{
    [CollectionDefinition(nameof(TPLTaskSchedulerTests), DisableParallelization = true)]

    public class TPLTaskSchedulerTests
    {
        private static TPLTaskScheduler sch = new TPLTaskScheduler();

        private void RefreshSchedulerInstance()
        {
            sch = null;
            GC.Collect();

            sch = new TPLTaskScheduler();
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void TPLTaskScheduler_Start_And_Enqueue_A_Task()
        {
            //ARRANGE
            var counter = 0;

            //ACT
            var task = Task.Factory.StartNew(
                () => { counter++; }
                , CancellationToken.None
                , TaskCreationOptions.None
                , sch);

            Task.WaitAll(task);

            //ASSERT
            counter.Should().BeGreaterThan(0);
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void TPLTaskScheduler_It_Should_Be_TypeOF_IDisposable()
        {
            //ACT, ASSERT
            sch.Should().BeAssignableTo<IDisposable>();
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void TPLTaskScheduler_It_Should_Be_TypeOF_ITaskScheduler()
        {
            //ACT, ASSERT
            sch.Should().BeAssignableTo<ITaskScheduler>();
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void TPLTaskSchedulerOnInit_Without_Passing_Any_Argument_It_Should_Init_With_DefaultValues()
        {
            //ACT, ASSERT
            sch.MaximumConcurrencyLevel.Should().Be(TPLConstants.TPL_SCHEDULER_MIN_CONS_COUNT);
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void TPLTaskSchedulerOnInit_When_Passing_ThreadCount_LessThanZero_It_Should_Throw_ArgumentOutOfRangeException()
        {
            //ARRANGE
            var threadCount = 0;
            Action action = () => new TPLTaskScheduler(consumersCount: threadCount);

            //ACT, ASSERT
            action.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void TPLTaskSchedulerOnInit_When_Passing_TaskDueTime_LessThanZero_It_Should_Throw_ArgumentOutOfRangeException()
        {
            //ARRANGE
            var taskDueTime = 0;
            Action action = () => new TPLTaskScheduler(taskDueTime: taskDueTime);

            //ACT, ASSERT
            action.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void TPLTaskSchedulerOnInit_When_CannotAddItems_It_Should_Throw_InvalidOperationException()
        {
            //ARRANGE
            var loggerMock = new Mock<ILogger>();
            var workMock = new Mock<IWorkItem>();
            var expectedError = Guid.NewGuid().ToString();
            var consCount = 5;
            var schedulerFake = new TPLTaskSchedulerCannotAddWorkItemsFake(loggerMock.Object, consumersCount: consCount);
            Action action = () => schedulerFake.EnqueueWork(workMock.Object);

            //ACT, ASSERT
            action.Should().Throw<InvalidOperationException>();
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void EnqueuedWorkItems_It_Should_Return_The_Pending_WorkItemList()
        {
            //ARRANGE
            var counter = 0;
            var items = new WorkItem[]
            {
                  new WorkItem(() => { counter++; })
                , new WorkItem(() => { counter++; })
                , new WorkItem(() => { counter++; })
                , new WorkItem(() => { counter++; })
            };

            sch.EnqueueWork(items);

            //ACT
            Task.WaitAll(items.Select(i => i.Task).ToArray());

            //ASSERT
            counter.Should().Be(items.Count());
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void EnqueuedWorkItems_With_No_WorkItems_It_Should_Return_Zero_Element()
        {
            //ACT
            var res = sch.EnqueuedWorkItems;

            //ASSERT
            res.Should().BeEmpty();
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void AllTasksCompleted_With_An_Empty_Queue_It_Should_Return_TrulyValue()
        {
            //ARRANGE
            var count = 0;
            var items = new WorkItem[]
            {
                  new WorkItem(() => { count++; })
                , new WorkItem(() => { count++; })
                , new WorkItem(() => { count++; })
                , new WorkItem(() => { count++; })
            };

            sch.EnqueueWork(items);

            //ACT
            Task.WaitAll(items.Select(i => i.Task).ToArray());

            //ASSERT
            sch.AllTasksCompleted.Should().BeTrue();
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void AllTasksCompleted_OnDispose_When_Adding_A_New_WorkItem_It_Should_InvalidOperationException()
        {
            //ARRANGE
            var items = new WorkItem[]
            {
                  new WorkItem(() => { Thread.Sleep(2*1000); })
                , new WorkItem(() => { Thread.Sleep(2*1000); })
                , new WorkItem(() => { Thread.Sleep(2*1000); })
                , new WorkItem(() => { Thread.Sleep(2*1000); })
            };

            Action action = () => sch.EnqueueWork(items);

            //ACT,ASSERT
            sch.Dispose();
            action.Should().Throw<ObjectDisposedException>();
            RefreshSchedulerInstance();
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void EnqueueWork_OnMaxItemQueue_reached_It_Should_Throw_InvalidOperationException()
        {
            //ARRANGE
            var schTemp = new TPLTaskScheduler(consumersCount: 1, maxQueueItems: 1);
            var items = new WorkItem[]
            {
                  new WorkItem(() => { Thread.Sleep(2*1000); })
                , new WorkItem(() => { Thread.Sleep(2*1000); })
                , new WorkItem(() => { Thread.Sleep(2*1000); })
                , new WorkItem(() => { Thread.Sleep(2*1000); })
            };

            Action action = () => schTemp.EnqueueWork(items);

            //ACT,ASSERT
            action.Should().Throw<InvalidOperationException>();

            schTemp.Dispose();
            schTemp = null;
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void EnqueueWork_When_WorkCallback_IsNull_It_Should_Throw_ArgumentNullException()
        {
            //ARRANGE
            Action doWork = null;
            Action action = () => sch.EnqueueWork(doWork);

            //ACT,ASSERT
            action.Should().Throw<ArgumentNullException>();
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void EnqueueWork_When_DueTime_IsLessOrEqualToZero_It_Should_Throw_ArgumentOutOfRangeException()
        {
            //ARRANGE
            Action action = () => sch.EnqueueWork(() => { /* do nothing */ }, dueTime: 0);

            //ACT,ASSERT
            action.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void EnqueueWork_When_TaskSchedulerDisposed_It_Should_Throw_ObjectDisposedException()
        {
            //ARRANGE
            Action action = () => sch.EnqueueWork(() => { /* do nothing */ });

            //ACT,ASSERT
            sch.Dispose();
            action.Should().Throw<ObjectDisposedException>();
            RefreshSchedulerInstance();
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void EnqueueWork_It_Should_Do_The_Work()
        {
            //ARRANGE
            var updateMe = string.Empty;
            sch.EnqueueWork(() => { updateMe = Guid.NewGuid().ToString(); });

            //ACT
            Thread.Sleep(2 * 1000);

            //ASSERT
            updateMe.Should().NotBeNullOrEmpty();
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void EnqueueWorkWithDoWorkCallback_When_WorkCallback_IsNull_It_Should_Throw_ArgumentNullException()
        {
            //ARRANGE
            Action doWork = null;
            Action action = () => sch.EnqueueWork(doWork, () => { /* do nothing */ });

            //ACT,ASSERT
            action.Should().Throw<ArgumentNullException>();
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void EnqueueWorkWithDoWorkCallback_When_DueTime_IsLessOrEqualToZero_It_Should_Throw_ArgumentOutOfRangeException()
        {
            //ARRANGE
            Action action = () => sch.EnqueueWork(
                () => { /* do nothing */ }
                , () => { /* do nothing */ }
                , dueTime: 0);

            //ACT,ASSERT
            action.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void EnqueueWorkWithDoWorkCallback_When_TaskSchedulerDisposed_It_Should_Throw_ObjectDisposedException()
        {
            //ARRANGE
            Action action = () => sch.EnqueueWork(
                () => { /* do nothing */ }
                , () => { /* do nothing */ });

            //ACT,ASSERT
            sch.Dispose();
            action.Should().Throw<ObjectDisposedException>();
            RefreshSchedulerInstance();
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void EnqueueWorkWithDoWorkCallback_It_Should_Do_The_Work()
        {
            //ARRANGE
            var updateMe = 0;
            sch.EnqueueWork(() => { updateMe++; }, () => { updateMe++; });

            //ACT, ASSERT
            Thread.Sleep(2 * 1000);
            updateMe.Should().BeGreaterOrEqualTo(2);
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void EnqueueWork_Passing_A_WorkItemByArgument_When_WorkCallback_IsNull_It_Should_Throw_ArgumentNullException()
        {
            //ARRANGE

            Action doWork = null;
            Action action = () => sch.EnqueueWork(doWork, () => { /* do nothing */ });

            //ACT,ASSERT
            action.Should().Throw<ArgumentNullException>();
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void EnqueueWork_Passing_A_NullWorkItemByArgument_It_Should_Throw_ArgumentNulleException()
        {
            //ARRANGE
            WorkItem work = null;
            Action action = () => sch.EnqueueWork(work);

            //ACT,ASSERT
            action.Should().Throw<ArgumentNullException>();
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void EnqueueWork_Passing_A_WorkItemByArgument_When_DueTime_IsLessOrEqualToZero_It_Should_Throw_ArgumentOutOfRangeException()
        {
            //ARRANGE

            Action action = () =>
            {
                var work = new WorkItem(() => { /* do nothing */}, dueTime: 0);
                sch.EnqueueWork(work);
            };

            //ACT,ASSERT
            action.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void EnqueueWork_Passing_A_WorkItemByArgument_When_TaskSchedulerDisposed_It_Should_Throw_ObjectDisposedException()
        {
            //ARRANGE

            var work = new WorkItem(() => { /* do nothing */});
            Action action = () => sch.EnqueueWork(work);

            //ACT,ASSERT
            sch.Dispose();
            action.Should().Throw<ObjectDisposedException>();
            RefreshSchedulerInstance();
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void EnqueueWork_Passing_A_WorkItemByArgument_It_Should_Do_The_Work()
        {
            //ARRANGE

            var updateMe = 0;
            var work = new WorkItem(() => { updateMe++; });
            sch.EnqueueWork(work);

            //ACT, ASSERT
            Thread.Sleep(2 * 1000);
            updateMe.Should().BeGreaterOrEqualTo(0);
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void EnqueueWork_Passing_An_Empty_WorkItemList_It_Should_Throw_ArgumentOutOfRangeException()
        {
            //ARRANGE

            Action action = () => sch.EnqueueWork(new List<IWorkItem>());

            //ACT,ASSERT
            action.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void EnqueueWork_Passing_An_Null_WorkItemList_It_Should_Throw_ArgumentNullException()
        {
            //ARRANGE

            List<IWorkItem> works = null;
            Action action = () => sch.EnqueueWork(works);

            //ACT,ASSERT
            action.Should().Throw<ArgumentNullException>();
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void TryExecuteItNow_Passing_Null_WorkItem_It_Should_Throw_ArgumentNulLException()
        {
            //ARRANGE

            IWorkItem work = null;
            Action action = () => sch.TryExecuteItNow(work);

            //ACT,ASSERT
            action.Should().Throw<ArgumentNullException>();
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void TryExecuteItNow_Passing_A_NonRunnable_WorkItem_It_Should_Return_A_FalsyValue()
        {
            ///ARRANGE

            IWorkItem work = new WorkItem(() => { /*do nothing*/ });

            //ACT
            work.SetCanceled();
            var res = sch.TryExecuteItNow(work);

            //ASSERT
            res.Should().BeFalse();
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void TryExecuteItNow_Passing_A_Runnable_WorkItem_It_Should_Complet_The_WorkItem()
        {
            //ARRANGE
            var counter = 0;

            IWorkItem work = new WorkItem(() => { counter++; });

            //ACT
            var res = sch.TryExecuteItNow(work);

            //ASSERT
            res.Should().BeTrue();
            counter.Should().BeGreaterThan(0);
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void TryExecuteItNow_When_WorkCallback_IsNull_It_Should_Throw_ArgumentNullException()
        {
            //ARRANGE

            Action doWork = null;
            Action action = () => sch.TryExecuteItNow(doWork);

            //ACT,ASSERT
            action.Should().Throw<ArgumentNullException>();
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void TryExecuteItNow_When_DueTime_IsLessOrEqualToZero_It_Should_Throw_ArgumentOutOfRangeException()
        {
            //ARRANGE

            Action action = () => sch.TryExecuteItNow(() => { /* do nothing */ }, dueTime: 0);

            //ACT,ASSERT
            action.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void TryExecuteItNow_When_TaskSchedulerDisposed_It_Should_Throw_ObjectDisposedException()
        {
            //ARRANGE

            Action action = () => sch.TryExecuteItNow(() => { /* do nothing */ });

            //ACT,ASSERT
            sch.Dispose();
            action.Should().Throw<ObjectDisposedException>();
            RefreshSchedulerInstance();
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void TryExecuteItNow_It_Should_Do_The_Work()
        {
            //ARRANGE

            var updateMe = string.Empty;
            sch.TryExecuteItNow(() => { updateMe = Guid.NewGuid().ToString(); });

            //ACT, ASSERT
            updateMe.Should().NotBeNullOrEmpty();
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void TryExecuteItNowWithDoWorkCallback_When_WorkCallback_IsNull_It_Should_Throw_ArgumentNullException()
        {
            //ARRANGE

            Action doWork = null;
            Action action = () => sch.TryExecuteItNow(doWork, () => { /* do nothing */ });

            //ACT,ASSERT
            action.Should().Throw<ArgumentNullException>();
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void TryExecuteItNowWithDoWorkCallback_It_Should_Call_The_DoWorkCallback()
        {
            //ARRANGE

            var counter = 0;
            sch.TryExecuteItNow(() => { counter++; }, () => { counter++; });

            //ACT, ASSERT
            Thread.Sleep(2 * 1000);
            counter.Should().BeGreaterOrEqualTo(2);
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void TryExecuteItNowWithDoWorkCallback_When_DueTime_IsLessOrEqualToZero_It_Should_Throw_ArgumentOutOfRangeException()
        {
            //ARRANGE

            Action action = () => sch.TryExecuteItNow(() => { /* do nothing */ }, () => { /* do nothing */ }, dueTime: 0);

            //ACT,ASSERT
            action.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void TryExecuteItNowWithDoWorkCallback_When_TaskSchedulerDisposed_It_Should_Throw_ObjectDisposedException()
        {
            //ARRANGE

            Action action = () => sch.TryExecuteItNow(() => { /* do nothing */ }, () => { /* do nothing */ });

            //ACT,ASSERT
            sch.Dispose();
            action.Should().Throw<ObjectDisposedException>();

            RefreshSchedulerInstance();
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void TryExecuteItNowWithDoWorkCallback_It_Should_Do_The_Work()
        {
            //ARRANGE

            var updateMe = 0;
            sch.TryExecuteItNow(() => { updateMe++; }, () => { updateMe++; });

            //ACT,ASSERT
            updateMe.Should().BeGreaterOrEqualTo(2);
        }

    }
}
