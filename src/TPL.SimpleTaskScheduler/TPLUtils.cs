using Serilog;
using Serilog.Events;

namespace TPL.SimpleTaskScheduler
{
    public static class TPLUtils
    {
        private static ILogger _Logger;
        public static ILogger GetLogger()
        {
            if (_Logger is null)
            {
                _Logger = new LoggerConfiguration()
#if DEBUG
                    .WriteTo.Debug(LogEventLevel.Verbose)
#else
                    .WriteTo.Debug(LogEventLevel.Information)
#endif
                    .CreateLogger();
            }

            return _Logger;
        }
    }
}
