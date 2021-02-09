using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using System.Xml;
using LambdaSharp.Logging;
using LambdaSharp.Records;
using Smylee.PlaylistMonitor.Library.Models;
using Xunit;

namespace Smylee.PlaylistMonitor.PlaylistCompare.Tests {

    public class Logger : ILambdaSharpLogger {
        private ILambdaSharpInfo _info;
        private bool _debugLoggingEnabled;

        public void Log(LambdaLogLevel level, Exception exception, string format, params object[] arguments) { }

        public void LogRecord(ALambdaLogRecord record) { }

        public ILambdaSharpInfo Info => _info;

        public bool DebugLoggingEnabled => _debugLoggingEnabled;
    }
}