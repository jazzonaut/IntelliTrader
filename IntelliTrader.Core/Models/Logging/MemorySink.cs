using System;
using System.IO;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;

namespace IntelliTrader.Core
{
    internal class MemorySink : ILogEventSink
    {
        readonly TextWriter textWriter;
        readonly ITextFormatter textFormatter;
        readonly object syncRoot = new object();

        public MemorySink(System.IO.TextWriter textWriter, ITextFormatter textFormatter)
        {
            this.textWriter = textWriter;
            this.textFormatter = textFormatter ?? throw new ArgumentNullException(nameof(textFormatter));
        }

        public void Emit(LogEvent logEvent)
        {
            if (logEvent == null) throw new ArgumentNullException(nameof(logEvent));

            lock (syncRoot)
            {
                textFormatter.Format(logEvent, textWriter);
                textWriter.Flush();
            }
        }
    }
}