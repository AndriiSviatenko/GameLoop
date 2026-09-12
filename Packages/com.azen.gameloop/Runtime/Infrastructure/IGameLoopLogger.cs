using System;

namespace Azen.GameLoop.Infrastructure
{
    public interface IGameLoopLogger
    {
        void Log(string message);
        void Warning(string message);
        void Error(string message, Exception exception = null);
    }
}
