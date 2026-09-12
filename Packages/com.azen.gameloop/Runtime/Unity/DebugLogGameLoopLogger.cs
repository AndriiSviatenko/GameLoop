using System;
using UnityEngine;

namespace Azen.GameLoop.Infrastructure
{
    public sealed class DebugLogGameLoopLogger : IGameLoopLogger
    {
        public static readonly DebugLogGameLoopLogger Instance = new DebugLogGameLoopLogger();

        private const string Prefix = "[GameLoop] ";

        public void Log(string message)     => Debug.Log(Prefix + message);
        public void Warning(string message) => Debug.LogWarning(Prefix + message);

        public void Error(string message, Exception exception = null)
        {
            Debug.LogError(Prefix + message);
            if (exception != null) Debug.LogException(exception);
        }
    }
}
