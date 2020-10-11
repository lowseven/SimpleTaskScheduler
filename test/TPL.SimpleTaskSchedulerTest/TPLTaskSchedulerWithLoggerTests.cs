using FluentAssertions;
using Moq;
using Serilog;
using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TPL.SimpleTaskScheduler;
using TPL.SimpleTaskSchedulerTest.Fakes;
using Xunit;

namespace TPL.SimpleTaskSchedulerTest
{

    [CollectionDefinition(nameof(TPLTaskSchedulerWithLoggerTests), DisableParallelization = true)]
    public class TPLTaskSchedulerWithLoggerTests
    {

        private static TPLTaskScheduler sch = new TPLTaskScheduler(TPLUtils.GetLogger());


        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void TPLTaskSchedulerOnInit_With_Logger_Argument_Without_Passing_Any_Argument_It_Should_Init_With_DefaultValues()
        {
            //ACT, ASSERT
            sch.MaximumConcurrencyLevel.Should().Be(TPLConstants.TPL_SCHEDULER_MIN_CONS_COUNT);
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void TPLTaskSchedulerOnInit_With_Logger_Argument_When_Passing_ThreadCount_LessThanZero_It_Should_Throw_ArgumentOutOfRangeException()
        {
            //ARRANGE
            var threadCount = 0;
            Action action = () => new TPLTaskScheduler(taskDueTime: threadCount);

            //ACT, ASSERT
            action.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void TPLTaskSchedulerOnInit_With_Logger_Argument_When_Passing_TaskDueTime_LessThanZero_It_Should_Throw_ArgumentOutOfRangeException()
        {
            //ARRANGE
            var taskDueTime = 0;
            Action action = () => new TPLTaskScheduler(taskDueTime: taskDueTime);

            //ACT, ASSERT
            action.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void TPLTaskScheduler_With_Logger_Argument_Start_And_Enqueue_A_Task()
        {
            //ARRANGE
            var counter = 0;

            //ACT
            Task.Factory.StartNew(
                () => { counter++; }
                , CancellationToken.None
                , TaskCreationOptions.None
                , sch)
            .Wait();

            //ASSERT
            counter.Should().BeGreaterThan(0);
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void TPLTaskScheduler_With_Logger_It_Should_LogDebug_Starting_ConsumerTasks()
        {
            //ARRANGE
            var consCount = 5;
            var loggerMock = new Mock<ILogger>();

            //ACT
            var scheduler = new TPLTaskScheduler(loggerMock.Object, consumersCount: consCount);
            loggerMock.Setup(i => i.Debug(
                It.Is<string>(m =>
                    m.Contains("starting", StringComparison.InvariantCultureIgnoreCase) && Regex.IsMatch(m, @"\d+"))));

            //ASSERT
            loggerMock.Verify(i => i.Debug(
                It.Is<string>(m =>
                    m.Contains("starting", StringComparison.InvariantCultureIgnoreCase) && Regex.IsMatch(m, @"\d+")))
            , Times.Exactly(consCount));
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void TPLTaskScheduler_With_Logger_And_InvalidTaskConsumer_It_Should_LogDebug_The_ConsumerTasks_Exception()
        {
            //ARRANGE
            var consCount = 5;
            var loggerMock = new Mock<ILogger>();

            //ACT
            var schedulerFake = new TPLTaskSchedulerWithInvalidConsTaskFake(
                loggerMock.Object
                , consCount
                , TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS
                , TPLConstants.TPL_SCHEDULER_MAX_QUEUE_ITEMS);

            loggerMock.Setup(i => i.Debug(
                It.Is<string>(m =>
                    m.Contains("Error", StringComparison.InvariantCultureIgnoreCase) && Regex.IsMatch(m, @"\d+"))));

            //ASSERT
            loggerMock.Verify(i => i.Debug(
                It.Is<string>(m =>
                    m.Contains("error", StringComparison.InvariantCultureIgnoreCase) && Regex.IsMatch(m, @"\d+")))
            , Times.Exactly(consCount));
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void TPLTaskScheduler_With_Logger_And_Invalid_CanceledTaskConsumer_It_Should_OperationCanceledException()
        {
            //ARRANGE
            var loggerMock = new Mock<ILogger>();
            var expectedError = Guid.NewGuid().ToString();

            TPLTaskSchedulerWithInvalidCanceledConsTaskFake.EXCEPTION_ERROR = expectedError;
            Action action = () =>
            {
                var schedulerFake = new TPLTaskSchedulerWithInvalidCanceledConsTaskFake(
                loggerMock.Object
                , TPLConstants.TPL_SCHEDULER_MIN_CONS_COUNT
                , TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS
                , TPLConstants.TPL_SCHEDULER_MAX_QUEUE_ITEMS);
            };

            //ACT, ASSERT
            action.Should()
                .Throw<OperationCanceledException>()
                .WithMessage(expectedError);
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void TPLTaskScheduler_With_Logger_And_CanceledTaskConsumer_It_Should_LogDebug_Exit_Status()
        {
            //ARRANGE
            var loggerMock = new Mock<ILogger>();
            var expectedError = Guid.NewGuid().ToString();
            var consCount = 5;
            var schedulerFake = new TPLTaskSchedulerWithCanceledConsTaskFake(loggerMock.Object, consumersCount: consCount);

            loggerMock.Setup(i => i.Debug(
                It.Is<string>(m =>
                    m.Contains("canceled", StringComparison.InvariantCultureIgnoreCase) && Regex.IsMatch(m, @"\d+"))));

            //ACT
            schedulerFake.CancelConsumerTask();

            //ASSERT
            loggerMock.Verify(i => i.Debug(
                It.Is<string>(m =>
                    m.Contains("canceled", StringComparison.InvariantCultureIgnoreCase) && Regex.IsMatch(m, @"\d+")))
            , Times.Exactly(consCount));
        }
    }
}
