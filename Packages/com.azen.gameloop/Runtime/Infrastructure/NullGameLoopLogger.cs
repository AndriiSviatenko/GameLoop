using System;

namespace Azen.GameLoop.Infrastructure
{
    public sealed class NullGameLoopLogger : IGameLoopLogger
    {
        public static readonly NullGameLoopLogger Instance = new NullGameLoopLogger();

        public void Log(string message) { }
        public void Warning(string message) { }
        public void Error(string message, Exception exception = null) { }
    }
}
