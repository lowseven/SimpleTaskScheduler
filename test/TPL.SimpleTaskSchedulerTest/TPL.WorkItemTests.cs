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
    public class TPLWorkItemTests
    {

        [Fact]
        public void WorkItem_Should_Be_TypeOf_IAwaitable()
        {
            //ARRANGE
            var type = typeof(IWorkItem);

            //ACT, ASSERT
            type.Should().BeAssignableTo(typeof(IAwaitable<IWorkItem>));
        }

        [Fact]
        public void WorkItem_Should_Be_TypeOf_INotifyCompletion()
        {
            //ARRANGE
            var type = typeof(IWorkItem);

            //ACT, ASSERT
            type.Should().BeAssignableTo(typeof(INotifyCompletion));
        }

        [Fact]
        public void WorkItem_Should_Be_TypeOf_IDisposable()
        {
            //ARRANGE
            var type = typeof(IWorkItem);

            //ACT, ASSERT
            type.Should().BeAssignableTo(typeof(IDisposable));
        }

        [Fact]
        public void WorkItem_DoWork_Should_NotBe_Null()
        {
            //ARRANGE
            Action action = () => { /*do nothing*/ };
            var work = new WorkItem(action);

            //ACT, ASSERT
            work.DoWork.Should().NotBeNull();
            work.DoWork.Should().BeSameAs(action);
        }

        [Fact]
        public void WorkItem_DoWork_And_Get_The_Result()
        {
            //ARRANGE
            Action action = () => { /*do nothing*/ };
            var work = new WorkItem(action);

            //ACT
            work.DoWork();
            work.SetResult();
            
            //ASSERT
            work.DoWork.Should().NotBeNull();
            work.IsCompleted.Should().BeTrue();
            (work.Task as Task<object>).Result.Should().BeNull();
        }

        [Fact]
        public void WorkItem_Passing_AnEmpty_DoWorkAction_It_Should_Throw_ArgumentNullException()
        {
            //ARRANGE
            Action doWork = null;
            Action action = () => new WorkItem(doWork, secsBeforeCanceling: 5);

            //ACT, ASSERT
            action.Should().ThrowExactly<ArgumentNullException>(nameof(doWork));
        }

        [Fact]
        public void WorkItem_Passing_SecsBeforeCanceling_LessThanZero_It_Should_Throw_ArgumentOutOfRangeException()
        {
            //ARRANGE
            Action doWork = () => { /*do nothing*/  };
            var secsBeforeCanceling = -1;
            Action action = () => new WorkItem(doWork, secsBeforeCanceling: secsBeforeCanceling);

            //ACT, ASSERT
            action.Should().ThrowExactly<ArgumentOutOfRangeException>(nameof(secsBeforeCanceling));
        }

        [Fact]
        public void WorkItem_When_CancellationToken_Is_Raised_It_Should_Cancel_The_WorkItem()
        {
            //ARRANGE
            var work = new WorkItem(() => { /*do nothing*/ });

            //ACT
            work.NotifyCancellation();

            //ASSERT
            work.IsCanceled.Should().BeTrue();
        }

        [Fact]
        public void WorkItemOnInit_It_Should_Have_A_NonZeroId()
        {
            //ARRANGE
            var item = new WorkItem(() => { /*do nothing*/ });

            //ACT, ASSERT
            item.Id.Should().BeGreaterThan(0);
        }

        [Fact]
        public void WorkItemOnInit_It_Should_Be_Valid()
        {
            //ARRANGE
            var item = new WorkItem(() => { /*do nothing*/ });

            //ACT, ASSERT
            item.IsValid.Should().BeTrue();
        }

        [Fact]
        public void WorkItemOnInit_When_Setting_A_CancellationElapsedTime_It_Should_Cancel_The_WorkItem_If_Not_Completed_In_Time()
        {
            //ARRANGE
            var item = new WorkItem(() => { Thread.Sleep(2*1000); }, secsBeforeCanceling: 1);

            //ACT
            item.DoWork();
            
            //ASSERT
            item.IsCanceled.Should().BeTrue();
            item.IsRunnable.Should().BeFalse();
            item.IsCompleted.Should().BeFalse();
        }

        [Fact]
        public void WorkItemOnInit_It_Should_Be_Runnable()
        {
            //ARRANGE
            var item = new WorkItem(() => { /*do nothing*/ });

            //ACT, ASSERT
            item.IsRunnable.Should().BeTrue();
            item.IsCanceled.Should().BeFalse();
            item.IsCompleted.Should().BeFalse();
        }

        [Fact]
        public void WorkItemOnDisposed_Calling_SetCanceledMethod_It_Should_ThrowObjectDisposedException()
        {
            //ARRANGE
            var item = new WorkItem(() => { /*do nothing*/ });
            Action action = () => item.SetCanceled();

            //ACT
            item.Dispose();

            //ASSERT
            action.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void SetCancelled_It_Should_Cancel_The_WorkItem()
        {
            //ARRANGE
            var item = new WorkItem(() => { /*do nothing*/ });

            //ACT
            item.SetCanceled();

            //ASSERT
            item.IsCanceled.Should().BeTrue();
        }

        [Fact]
        public void WorkItemOnDisposed_Calling_SetResultMethod_It_Should_ThrowObjectDisposedException()
        {
            //ARRANGE
            var item = new WorkItem(() => { /*do nothing*/ });
            Action action = () => item.SetResult();

            //ACT
            item.Dispose();

            //ASSERT
            action.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void WorkItemOnCancellationRequested_Calling_SetResultionMethod_It_Should_ThrowOperationCanceledException()
        {
            //ARRANGE
            var item = new WorkItem(() => { /*do nothing*/ });
            Action action = () => item.SetResult();

            //ACT
            item.NotifyCancellation();

            //ASSERT
            action.Should().Throw<OperationCanceledException>();
        }

        [Fact]
        public void SetResult_It_Should_Set_The_ResultInto_WorkItemTask()
        {
            //ARRANGE
            var item = new WorkItem(() => { /*do nothing*/ });

            //ACT
            item.SetResult();

            //ASSERT
            (item.Task as Task<object>).Result.Should().BeNull();
        }

        [Fact]
        public void WorkItemOnDisposed_Calling_SetExceptionMethod_It_Should_ThrowObjectDisposedException()
        {
            //ARRANGE
            var item = new WorkItem(() => { /*do nothing*/ });
            Action action = () => item.SetException(null);

            //ACT
            item.Dispose();

            //ASSERT
            action.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void WorkItemOnCancellationRequested_Calling_SetExceptionMethod_It_Should_ThrowOperationCanceledException()
        {
            //ARRANGE
            var item = new WorkItem(() => { /*do nothing*/ });
            Action action = () => item.SetException(null);

            //ACT
            item.NotifyCancellation();

            //ASSERT
            action.Should().Throw<OperationCanceledException>();
        }

        [Fact]
        public void SetException_It_Should_Set_The_ExceptionInto_WorkItemTask()
        {
            //ARRANGE
            var item = new WorkItem(() => { /*do nothing*/ });
            var expectedRes = new Exception(nameof(WorkItem));

            //ACT
            item.SetException(expectedRes);

            //ASSERT
            item.Task.Exception.InnerException.Should().Be(expectedRes);
        }

        [Fact]
        public void WorkItemOnDisposed_Calling_SetOnCompletedMethod_It_Should_ThrowObjectDisposedException()
        {
            //ARRANGE
            var item = new WorkItem(() => { /*do nothing*/ });
            Action action = () => item.OnCompleted(null);

            //ACT
            item.Dispose();

            //ASSERT
            action.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void WorkItemOnCancellationRequested_Calling_SetCompletedMethod_It_Should_ThrowOperationCanceledException()
        {
            //ARRANGE
            var item = new WorkItem(() => { /*do nothing*/ });
            Action action = () => item.OnCompleted(null);

            //ACT
            item.NotifyCancellation();

            //ASSERT
            action.Should().Throw<OperationCanceledException>();
        }

        [Fact]
        public void OnCompleted_And_ContinuationCallback_Is_Null_It_Should_SetCompletion()
        {
            //ARRANGE
            var item = new WorkItem(() => { /*do nothing*/ });

            //ACT
            item.SetResult();
            item.OnCompleted(null);

            //ASSERT
            (item.Task as Task<object>).Result.Should().BeNull();
        }

        [Fact]
        public void OnCompleted_It_Should_Call_The_WorkItemTask_ContinuationCallback()
        {
            //ARRANGE
            var item = new WorkItem(() => { /*do nothing*/ });
            var expectedRes = new Exception(nameof(WorkItem));
            var action = new Mock<Action>();

            //ACT
            item.SetResult();
            item.OnCompleted(action.Object);
            action.Verify(i => i.Invoke(), Times.Once);

            //ASSERT
            item.IsCompleted.Should().BeTrue();
        }

        [Fact]
        public void WorkItemOnDisposed_Calling_SetGetResultMethod_It_Should_ThrowObjectDisposedException()
        {
            //ARRANGE
            var item = new WorkItem(() => { /*do nothing*/ });
            Action action = () => item.GetResult();

            //ACT
            item.Dispose();

            //ASSERT
            action.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void WorkItemOnCancellationRequested_Calling_GetResultMethod_It_Should_ThrowOperationCanceledException()
        {
            //ARRANGE
            var work = new Mock<Action>();
            var item = new WorkItem(work.Object);
            Action action = () => item.GetResult();

            //ACT
            item.NotifyCancellation();
            work.Verify(i => i.Invoke(), Times.Never);

            //ASSERT
            action.Should().Throw<OperationCanceledException>();
        }

        [Fact]
        public void GetResult_It_Should_Set_The_Result_Into_WorkItemTask()
        {
            //ARRANGE
            var item = new WorkItem(() => { /*do nothing*/ });

            //ACT
            item.SetResult();
            item.GetResult();

            //ASSERT
            (item.Task as Task<object>).Result.Should().BeNull();
        }

        [Fact]
        public void WorkItemOnDisposed_Calling_GetAwaiterMethod_It_Should_ThrowObjectDisposedException()
        {
            //ARRANGE
            var work = new WorkItem(() => { /*do nothing*/ });
            Action action = () => work.GetAwaiter();

            //ACT, ASSERT
            work.Dispose();
            action.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
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

        [Fact]
        public void GetAwaiter_It_Should_Get_An_Object_With_The_AwaiterMethods()
        {
            //ARRANGE
            var work = new Mock<Action>();
            var item = new WorkItem(work.Object);

            //ACT
            var obj = item.GetAwaiter();
            work.Verify(i => i.Invoke(), Times.Once);

            //ASSERT
            obj.Should().BeAssignableTo<IAwaitable<IWorkItem>>();
        }

        [Fact]
        public async void WorkItem_Can_Be_Awaitable()
        {
            //ARRANGE
            var work = new WorkItem(() => { /*do nothing*/ });

            //ACT
            await work;

            //ASSERT|
            work.IsCompleted.Should().BeTrue();
        }

        [Fact]
        public void WorkItem_Can_Be_Awaitable_And_When_Setting_A_CancellationElapsedTime_It_Should_Cancel_The_WorkItem_If_Not_Completed_In_Time()
        {
            //ARRANGE
            var item = new WorkItem(() => { Thread.Sleep(2 * 1000); }, secsBeforeCanceling: 1);
            Func<Task> a = async () => await item;

            //ACT,ASSERT
            a.Should().ThrowAsync<OperationCanceledException>();
            item.IsCanceled.Should().BeTrue();
            item.IsRunnable.Should().BeFalse();
            item.IsCompleted.Should().BeFalse();
        }
    }
}