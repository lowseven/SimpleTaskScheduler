using FluentAssertions;
using Moq;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using TPL.Interfaces;
using TPL.SimpleTaskScheduler;
using Xunit;

namespace TPL.SimpleTaskSchedulerTest
{
    [CollectionDefinition(nameof(TPLTaskSchedulerTests), DisableParallelization = true)]
    public class TPLWorkItemWithDataTests
    {

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void WorkItem_Should_Be_TypeOf_IAwaitableAsync()
        {
            //ARRANGE
            var type = typeof(IWorkItem<object>);

            //ACT, ASSERT
            type.Should().BeAssignableTo(typeof(IAwaitable<IWorkItem>));
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void WorkItem_Should_Be_TypeOf_INotifyCompletion()
        {
            //ARRANGE
            var type = typeof(IWorkItem<object>);

            //ACT, ASSERT
            type.Should().BeAssignableTo(typeof(INotifyCompletion));
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void WorkItem_Should_Be_TypeOf_IDisposable()
        {
            //ARRANGE
            var type = typeof(IWorkItem<object>);

            //ACT, ASSERT
            type.Should().BeAssignableTo(typeof(IDisposable));
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void WorkItem_DoWork_Should_NotBe_Null()
        {
            //ARRANGE
            Func<object> action = () => new object();
            var work = new WorkItem<object>(action);

            //ACT, ASSERT
            work.DoWork.Should().NotBeNull();
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void WorkItem_DoWork_And_Get_The_Result()
        {
            //ARRANGE
            var expectedRes = new object();
            var work = new WorkItem<object>(() => expectedRes);

            //ACT
            work.DoWork();
            work.SetResult();

            //ASSERT
            work.DoWork.Should().NotBeNull();
            work.IsCompleted.Should().BeTrue();
            work.Result.Should().BeSameAs(expectedRes);
        }


        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void WorkItem_Passing_AnEmpty_DoWorkAction_It_Should_Throw_ArgumentNullException()
        {
            //ARRANGE
            Func<object> doWork = null;
            Action action = () => new WorkItem<object>(doWork, dueTime: 5);

            //ACT, ASSERT
            action.Should().ThrowExactly<ArgumentNullException>(nameof(doWork));
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void WorkItem_Passing_SecsBeforeCanceling_LessThanZero_It_Should_Throw_ArgumentOutOfRangeException()
        {
            //ARRANGE
            var secsBeforeCanceling = -1;
            Func<object> doWork = () => new object();
            Action action = () => new WorkItem<object>(doWork, dueTime: secsBeforeCanceling);

            //ACT, ASSERT
            action.Should().ThrowExactly<ArgumentOutOfRangeException>(nameof(secsBeforeCanceling));
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void WorkItemOnInit_It_Should_Have_A_NonZeroId()
        {
            //ARRANGE
            var item = new WorkItem<object>(() => new object());

            //ACT, ASSERT
            item.Id.Should().BeGreaterThan(0);
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void WorkItemOnInit_It_Should_Be_Valid()
        {
            //ARRANGE
            var item = new WorkItem<object>(() => new object());

            //ACT, ASSERT
            item.IsValid.Should().BeTrue();
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void WorkItemOnInit_It_Should_Be_Runnable()
        {
            //ARRANGE
            var item = new WorkItem<object>(() => new object());

            //ACT, ASSERT
            item.IsRunnable.Should().BeTrue();
            item.IsCanceled.Should().BeFalse();
            item.IsCompleted.Should().BeFalse();
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void WorkItemOnDisposed_Calling_SetCanceledMethod_It_Should_ThrowObjectDisposedException()
        {
            //ARRANGE
            var item = new WorkItem<object>(() => new object());
            Action action = () => item.SetCanceled();

            //ACT
            item.Dispose();

            //ASSERT
            action.Should().Throw<ObjectDisposedException>();
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public async void WorkItemOnAwaitable_It_Should_Return_The_Result()
        {
            //ARRANGE
            var item = new WorkItem<string>(() => Guid.NewGuid().ToString());

            //ACT
            var res = await item;

            //ASSERT
            res.Should().Be(item.Result);
        }


        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void SetCancelled_It_Should_Cancel_The_WorkItem()
        {
            //ARRANGE
            var item = new WorkItem<object>(() => new object());

            //ACT
            item.SetCanceled();

            //ASSERT
            item.IsCanceled.Should().BeTrue();
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void SetResult_It_Should_Set_The_ResultInto_WorkItemTask()
        {
            //ARRANGE
            var item = new WorkItem<object>(() => new object());

            //ACT
            item.SetResult();

            //ASSERT
            (item.Task as Task<object>).Result.Should().BeNull();
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void WorkItemOnDisposed_Calling_SetExceptionMethod_It_Should_ThrowObjectDisposedException()
        {
            //ARRANGE
            var item = new WorkItem<object>(() => new object());
            Action action = () => item.SetException(null);

            //ACT
            item.Dispose();

            //ASSERT
            action.Should().Throw<ObjectDisposedException>();
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void WorkItemOnCancellationRequested_Calling_SetExceptionMethod_It_Should_ThrowOperationCanceledException()
        {
            //ARRANGE
            var item = new WorkItem<object>(() => new object());
            Action action = () => item.SetException(null);

            //ACT
            item.NotifyCancellation();

            //ASSERT
            action.Should().Throw<OperationCanceledException>();
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void SetException_It_Should_Set_The_ExceptionInto_WorkItemTask()
        {
            //ARRANGE
            var item = new WorkItem<object>(() => new object());
            var expectedRes = new Exception(nameof(WorkItem));

            //ACT
            item.SetException(expectedRes);

            //ASSERT
            item.Task.Exception.InnerException.Should().Be(expectedRes);
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void WorkItemOnDisposed_Calling_SetOnCompletedMethod_It_Should_ThrowObjectDisposedException()
        {
            //ARRANGE
            var item = new WorkItem<object>(() => new object());
            Action action = () => item.OnCompleted(null);

            //ACT
            item.Dispose();

            //ASSERT
            action.Should().Throw<ObjectDisposedException>();
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void WorkItemOnCancellationRequested_Calling_SetCompletedMethod_It_Should_ThrowOperationCanceledException()
        {
            //ARRANGE
            var item = new WorkItem<object>(() => new object());
            Action action = () => item.OnCompleted(null);

            //ACT
            item.NotifyCancellation();

            //ASSERT
            action.Should().Throw<OperationCanceledException>();
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void OnCompleted_And_ContinuationCallback_Is_Null_It_Should_SetCompletion()
        {
            //ARRANGE
            var item = new WorkItem<object>(() => new object());

            //ACT
            item.SetResult();
            item.OnCompleted(null);

            //ASSERT
            (item.Task as Task<object>).Result.Should().BeNull();
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void OnCompleted_It_Should_Call_The_WorkItemTask_ContinuationCallback()
        {
            //ARRANGE
            var item = new WorkItem<object>(() => new object());

            //ACT
            item.SetResult();
            item.OnCompleted(null);

            //ASSERT
            item.IsCompleted.Should().BeTrue();
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void WorkItemOnDisposed_Calling_SetGetResultMethod_It_Should_ThrowObjectDisposedException()
        {
            //ARRANGE
            var item = new WorkItem<object>(() => new object());
            Action action = () => item.GetResult();

            //ACT
            item.Dispose();

            //ASSERT
            action.Should().Throw<ObjectDisposedException>();
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void GetResult_It_Should_Set_The_Result_Into_WorkItemTask()
        {
            //ARRANGE
            var item = new WorkItem<object>(() => new object());

            //ACT
            item.SetResult();
            item.GetResult();

            //ASSERT
            item.Result.Should().BeNull();
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void WorkItemOnDisposed_Calling_GetAwaiterMethod_It_Should_ThrowObjectDisposedException()
        {
            //ARRANGE
            var work = new WorkItem<object>(() => new object());
            Action action = () => work.GetAwaiter();

            //ACT, ASSERT
            work.Dispose();
            action.Should().Throw<ObjectDisposedException>();
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void WorkItemOnCancellationRequested_Calling_GetAwaiterMethod_It_Should_ThrowOperationCanceledException()
        {
            //ARRANGE
            var work = new Mock<Action>();
            var item = new WorkItem(work.Object);
            Action action = () => item.GetAwaiter();

            //ACT
            item.NotifyCancellation();
            work.Verify(i => i.Invoke(), Times.Never);

            //ASSERT
            action.Should().Throw<OperationCanceledException>();
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void GetAwaiter_It_Should_Get_An_Object_With_The_AwaiterMethods()
        {
            //ARRANGE
            var work = new Mock<Func<object>>();
            var item = new WorkItem<object>(work.Object);

            work.Setup(i => i());

            //ACT
            var obj = item.GetAwaiter();
            work.Verify(i => i(), Times.Once);

            //ASSERT
            obj.Should().BeAssignableTo<IAwaitable<IWorkItem>>();
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public async void WorkItem_Can_Be_Awaitable()
        {
            //ARRANGE
            var work = new WorkItem<object>(() => new object());

            //ACT
            await work;

            //ASSERT
            work.IsCompleted.Should().BeTrue();
            (work.Task as Task<object>).Result.Should().NotBeNull();
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void WorkItem_Can_Be_Awaitable_And_When_Setting_A_CancellationElapsedTime_It_Should_Cancel_The_WorkItem_If_Not_Completed_In_Time()
        {
            //ARRANGE
            var item = new WorkItem<object>(() => { Thread.Sleep(4 * 1000); return new object(); }, dueTime: 1);
            Func<Task> action = async () => { await item; };

            //ACT,ASSERT
            action.Should().ThrowAsync<OperationCanceledException>();
            item.IsCanceled.Should().BeTrue();
            item.IsRunnable.Should().BeFalse();
            item.IsCompleted.Should().BeFalse();
        }
    }
}
