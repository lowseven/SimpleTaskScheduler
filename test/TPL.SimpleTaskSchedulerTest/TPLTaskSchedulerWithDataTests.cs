using FluentAssertions;
using Moq;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TPL.Interfaces;
using TPL.SimpleTaskScheduler;
using Xunit;

namespace TPL.SimpleTaskSchedulerTest
{
    [CollectionDefinition(nameof(TPLTaskSchedulerWithDataTests), DisableParallelization = true)]
    public class TPLTaskSchedulerWithDataTests
    {
        private static TPLTaskScheduler<string> sch = new TPLTaskScheduler<string>();

        private void RefreshSchedulerInstance()
        {
            sch = null;
            GC.Collect();

            sch = new TPLTaskScheduler<string>();
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void TPLTaskSchedulerWithData_Start_And_Enqueue_A_Task()
        {
            //ARRANGE
            var expectedValue = Guid.NewGuid().ToString();
            var value = string.Empty;

            //ACT
            var resTask = Task.Factory.StartNew(
                () => { value = expectedValue; }
                , CancellationToken.None
                , TaskCreationOptions.None
                , sch);

            //ASSERT
            Thread.Sleep(2 * 1000);
            value.Should().Be(expectedValue);
            resTask.IsCompleted.Should().BeTrue();
            resTask.IsCompletedSuccessfully.Should().BeTrue();
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void TPLTaskSchedulerWithData_Start_And_Enqueue_A_Task_With_Its_Value()
        {
            //ARRANGE
            var expectedValue = Guid.NewGuid().ToString();
            var value = string.Empty;

            //ACT
            var resTask = Task.Factory.StartNew(
                () => { value = expectedValue; return value; }
                , CancellationToken.None
                , TaskCreationOptions.None
                , sch);

            //ASSERT
            Thread.Sleep(2 * 1000);
            value.Should().Be(expectedValue);
            resTask.IsCompleted.Should().BeTrue();
            resTask.IsCompletedSuccessfully.Should().BeTrue();

            var valueTask = resTask as Task<string>;
            valueTask.Should().NotBeNull();
            valueTask.Result.Should().Be(expectedValue);
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void TPLTaskSchedulerWithData_It_Should_Be_TypeOF_IDisposable()
        {
            //ACT, ASSERT
            sch.Should().BeAssignableTo<IDisposable>();
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void TPLTaskSchedulerWithData_It_Should_Be_TypeOF_ITaskScheduler()
        {
            //ACT, ASSERT
            sch.Should().BeAssignableTo<ITaskScheduler>();
            sch.Should().BeAssignableTo<ITaskScheduler<string>>();
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void TPLTaskSchedulerWithDataOnInit_Without_Passing_Any_Argument_It_Should_Init_With_DefaultValues()
        {
            //ACT, ASSERT
            sch.MaximumConcurrencyLevel.Should().Be(TPLConstants.TPL_SCHEDULER_MIN_CONS_COUNT);
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void TPLTaskSchedulerWithDataOnInit_When_Passing_ThreadCount_LessThanZero_It_Should_Throw_ArgumentOutOfRangeException()
        {
            //ARRANGE
            var threadCount = 0;
            Action action = () => new TPLTaskScheduler<string>(consumersCount: threadCount);

            //ACT, ASSERT
            action.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void TPLTaskSchedulerWithDataOnInit_When_Passing_TaskDueTime_LessThanZero_It_Should_Throw_ArgumentOutOfRangeException()
        {
            //ARRANGE
            var taskDueTime = 0;
            Action action = () => new TPLTaskScheduler<string>(dueTime: taskDueTime);

            //ACT, ASSERT
            action.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void EnqueuedWorkItems_It_Should_Return_The_Pending_WorkItemList()
        {
            //ARRANGE
            var counter = 0;
            var expectedValue = "0123";
            var value = string.Empty;
            var items = new WorkItem<string>[]
            {
                  new WorkItem<string>(() => (counter++).ToString())
                , new WorkItem<string>(() => (counter++).ToString())
                , new WorkItem<string>(() => (counter++).ToString())
                , new WorkItem<string>(() => (counter++).ToString())
            };

            sch.EnqueueWork(items);

            //ACT, //ASSERT
            Thread.Sleep(2 * 1000);
            items.All(i => expectedValue.Contains(i.Result)).Should().BeTrue();
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
        public void AllTasksCompleted_If_Queue_Empty_It_Should_TrulyValue()
        {
            //ACT,ASSERT
            sch.AllTasksCompleted.Should().BeTrue();
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void AllTasksCompleted_With_An_Empty_Queue_It_Should_Return_TrulyValue()
        {
            //ARRANGE
            var schTemp = new TPLTaskScheduler<string>(consumersCount: 1);
            var items = new WorkItem<string>[]
            {
                  new WorkItem<string>(() => { Thread.Sleep(2*1000); return string.Empty; })
                , new WorkItem<string>(() => { Thread.Sleep(2*1000); return string.Empty; })
                , new WorkItem<string>(() => { Thread.Sleep(2*1000); return string.Empty; })
                , new WorkItem<string>(() => { Thread.Sleep(2*1000); return string.Empty; })
            };

            schTemp.EnqueueWork(items);

            //ACT,ASSERT
            schTemp.AllTasksCompleted.Should().BeFalse();
            schTemp.Dispose();
            schTemp = null;
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void AllTasksCompleted_OnDispose_When_Adding_A_New_WorkItem_It_Should_InvalidOperationException()
        {
            //ARRANGE
            var items = new WorkItem<string>[]
            {
                  new WorkItem<string>(() => { Thread.Sleep(2*1000); return string.Empty; })
                , new WorkItem<string>(() => { Thread.Sleep(2*1000); return string.Empty; })
                , new WorkItem<string>(() => { Thread.Sleep(2*1000); return string.Empty; })
                , new WorkItem<string>(() => { Thread.Sleep(2*1000); return string.Empty; })
            };

            Action action = () => sch.EnqueueWork(items);

            //ACT,ASSERT
            sch.Dispose();
            action.Should().Throw<ObjectDisposedException>();
            RefreshSchedulerInstance();
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void EnqueueWork_OnMaxItemQueue_Reached_It_Should_Throw_InvalidOperationException()
        {
            //ARRANGE
            var schTemp = new TPLTaskScheduler<string>(consumersCount: 1, maxQueueItems: 1);
            var items = new WorkItem<string>[]
            {
                  new WorkItem<string>(() => { Thread.Sleep(2*1000); return string.Empty; })
                , new WorkItem<string>(() => { Thread.Sleep(2*1000); return string.Empty; })
                , new WorkItem<string>(() => { Thread.Sleep(2*1000); return string.Empty; })
                , new WorkItem<string>(() => { Thread.Sleep(2*1000); return string.Empty; })
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
            Func<string> doWork = null;
            Action action = () => sch.EnqueueWork(doWork);

            //ACT,ASSERT
            action.Should().Throw<ArgumentNullException>();
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void EnqueueWork_When_DueTime_Is_LowerThanZero_IsNull_It_Should_Throw_ArgumentOutOfRangeException()
        {
            //ARRANGE
            Func<string> doWork = () => string.Empty;
            Action action = () => sch.EnqueueWork(doWork, dueTime: -1);

            //ACT,ASSERT
            action.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void EnqueueWorkWithCallback_When_WorkCallback_IsNull_It_Should_Throw_ArgumentNullException()
        {
            //ARRANGE
            var exRes = Guid.NewGuid().ToString();
            var onDoneMock = new Mock<Action<string>>();
            Func<string> doWork = null;
            Action action = () => sch.EnqueueWork(doWork, onDoneMock.Object);

            //ACT,ASSERT
            onDoneMock.Verify(i => i(
                It.Is<string>(m => m.Equals(exRes)))
            , Times.Never);
            action.Should().Throw<ArgumentNullException>();
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void EnqueueWorkWithCallback_It_Should_Get_The_Result_In_The_OnDoneCallback_Argument()
        {
            //ARRANGE
            var exRes = Guid.NewGuid().ToString();
            var value = string.Empty;
            var onDoneMock = new Mock<Action<string>>();
            Func<string> doWork = () => exRes;

            //ACT
            sch.EnqueueWork(doWork, (res) => value = res);
            Thread.Sleep(2 * 1000);

            //ASSERT
            value.Should().Be(exRes);
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void EnqueueWorkWithCallback_When_DueTime_Is_LowerThanZero_IsNull_It_Should_Throw_ArgumentOutOfRangeException()
        {
            //ARRANGE
            var exRes = Guid.NewGuid().ToString();
            var value = string.Empty;
            var onDoneMock = new Mock<Action<string>>();
            Func<string> doWork = () => exRes;
            Action action = () => sch.EnqueueWork(doWork, (res) => value = res, dueTime: -1);

            //ACT, ASSERT
            action.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void EnqueueWork_Passing_A_Null_WorkItem_When_DueTime_It_Should_Throw_ArgumentNullException()
        {
            //ARRANGE
            IWorkItem<string> work = null;
            Action action = () => sch.EnqueueWork(work);

            //ACT, ASSERT
            action.Should().Throw<ArgumentNullException>();
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void EnqueueWork_Passing_A_WorkItem_It_Should_Do_The_Queued_Work()
        {
            //ARRANGE
            var res = string.Empty;
            var exRes = Guid.NewGuid().ToString();
            var work = new WorkItem<string>(() => res = exRes);

            //ACT
            sch.EnqueueWork(work);
            Thread.Sleep(2 * 1000);

            //ASSERT
            res.Should().Be(exRes);
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void TryExecuteWorkNow_Passing_A_Null_WorkItem_When_DueTime_It_Should_Throw_ArgumentNullException()
        {
            //ARRANGE
            IWorkItem<string> work = null;
            Action action = () => sch.TryExecuteItNow(work);

            //ACT, ASSERT
            action.Should().Throw<ArgumentNullException>();
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void TryExecuteWorkNow_Passing_A_WorkItem_It_Should_Do_The_Queued_Work()
        {
            //ARRANGE
            var res = string.Empty;
            var exRes = Guid.NewGuid().ToString();
            var work = new WorkItem<string>(() => res = exRes);

            //ACT
            sch.TryExecuteItNow(work);

            //ASSERT
            res.Should().Be(exRes);
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void TryExecuteItNow_Passing_Null_WorkItem_It_Should_Throw_ArgumentNulLException()
        {
            //ARRANGE
            IWorkItem<string> work = null;
            Action action = () => sch.TryExecuteItNow(work);

            //ACT,ASSERT
            action.Should().Throw<ArgumentNullException>();
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void TryExecuteItNow_Passing_A_NonRunnable_WorkItem_It_Should_Return_A_FalsyValue()
        {
            //ARRANGE
            IWorkItem<string> work = new WorkItem<string>(() => string.Empty);

            //ACT
            work.SetCanceled();
            var res = sch.TryExecuteItNow(work);

            //ASSERT
            res.Should().BeFalse();
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void TryExecuteItNow_When_WorkCallback_IsNull_It_Should_Throw_ArgumentNullException()
        {
            //ARRANGE
            Func<string> doWork = null;
            Action action = () => sch.TryExecuteItNow(doWork);

            //ACT,ASSERT
            action.Should().Throw<ArgumentNullException>();
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void TryExecuteItNow_When_DueTime_IsLessOrEqualToZero_It_Should_Throw_ArgumentOutOfRangeException()
        {
            //ARRANGE
            Action action = () => sch.TryExecuteItNow(() => string.Empty, dueTime: 0);

            //ACT,ASSERT
            action.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void TryExecuteItNow_When_TaskSchedulerDisposed_It_Should_Throw_ObjectDisposedException()
        {
            //ARRANGE
            Action action = () => sch.TryExecuteItNow(() => string.Empty);

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
            sch.TryExecuteItNow(() => updateMe = Guid.NewGuid().ToString());

            //ACT, ASSERT
            updateMe.Should().NotBeNullOrEmpty();
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void TryExecuteItNowWithDoWorkCallback_When_WorkCallback_IsNull_It_Should_Throw_ArgumentNullException()
        {
            //ARRANGE
            Func<string> doWork = null;
            Action action = () => sch.TryExecuteItNow(doWork, (r) => { /* do nothing */ });

            //ACT,ASSERT
            action.Should().Throw<ArgumentNullException>();
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void TryExecuteItNowWithDoWorkCallback_It_Should_Call_The_DoWorkCallback()
        {
            //ARRANGE
            var exRes = Guid.NewGuid().ToString();
            var value = string.Empty;

            //ACT
            sch.TryExecuteItNow(() => exRes, (r) => { value = exRes; });

            //ASSERT
            value.Should().Be(exRes);
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void TryExecuteItNowWithDoWorkCallback_When_DueTime_IsLessOrEqualToZero_It_Should_Throw_ArgumentOutOfRangeException()
        {
            //ARRANGE

            Action action = () => sch.TryExecuteItNow(() => string.Empty, (r) => { /* do nothing */ }, dueTime: 0);

            //ACT,ASSERT
            action.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void TryExecuteItNowWithDoWorkCallback_When_TaskSchedulerDisposed_It_Should_Throw_ObjectDisposedException()
        {
            //ARRANGE
            Action action = () => sch.TryExecuteItNow(() => string.Empty, (r) => { /* do nothing */ });

            //ACT,ASSERT
            sch.Dispose();
            action.Should().Throw<ObjectDisposedException>();

            RefreshSchedulerInstance();
        }
    }
}
