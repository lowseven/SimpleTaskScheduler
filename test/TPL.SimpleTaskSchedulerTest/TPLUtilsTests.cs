using FluentAssertions;
using Serilog;
using TPL.SimpleTaskScheduler;
using Xunit;

namespace TPL.SimpleTaskSchedulerTest
{
    [CollectionDefinition(nameof(TPLUtilsTests), DisableParallelization = true)]
    public class TPLUtilsTests
    {
        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void GetLogger_It_Should_Be_TYpeOf_Serilog_Logger()
        {
            //ARRANGE
            var log = TPLUtils.GetLogger();
            
            //ACT,ASSERT
            log.Should().BeAssignableTo<ILogger>();
        }

        [Fact(Timeout = TPLConstants.TPL_SCHEDULER_MIN_WAIT_SECONDS * TPLConstants.TPL_SCHEDULER_SECONDS_MULTI)]
        public void GetLogger_It_Should_Get_The_Logger()
        {
            //ARRANGE
            var log = TPLUtils.GetLogger();

            //ACT,ASSERT
            log.Should().NotBeNull();
        }
    }
}
