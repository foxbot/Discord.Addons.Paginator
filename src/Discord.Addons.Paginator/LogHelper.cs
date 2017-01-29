using System;

namespace Discord.Addons.Paginator
{
    internal class Log
    {
        private readonly string source;
        internal Log(string source)
        {
            this.source = source;
        }

        public LogMessage Debug(string text, Exception e = null) =>
            new LogMessage(LogSeverity.Debug, source, text, e);
        public LogMessage Verbose(string text, Exception e = null) =>
            new LogMessage(LogSeverity.Verbose, source, text, e);
        public LogMessage Info(string text, Exception e = null) =>
            new LogMessage(LogSeverity.Info, source, text, e);
        public LogMessage Warning(string text, Exception e = null) =>
            new LogMessage(LogSeverity.Warning, source, text, e);
        public LogMessage Error(string text, Exception e = null) =>
            new LogMessage(LogSeverity.Error, source, text, e);
        public LogMessage Critical(string text, Exception e = null) =>
            new LogMessage(LogSeverity.Critical, source, text, e);
    }
}
