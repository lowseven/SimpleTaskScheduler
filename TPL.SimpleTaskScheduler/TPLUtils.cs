using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using System;
using System.Collections.Generic;
using System.Text;

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
                    .WriteTo.Console(LogEventLevel.Debug)
#if DEBUG
                    .WriteTo.Debug(LogEventLevel.Verbose)
#endif
                    //.WriteTo.RollingFile(LogEventLevel.Debug, TPLConstants.TPL_)
                    .CreateLogger();
            }

            return _Logger;
        }
    }
}
