using System;
using System.Collections.Generic;
using System.Threading;
using Azen.GameLoop.Infrastructure;
using Cysharp.Threading.Tasks;
using NUnit.Framework;

namespace Azen.GameLoop.Tests
{
    internal sealed class FakeTime : ITimeProvider
    {
        public float DeltaTime { get; set; } = 0.1f;
        public float FixedDeltaTime { get; set; } = 0.02f;
    }

    internal sealed class CapturingLogger : IGameLoopLogger
    {
        public readonly List<Exception> Errors = new();
        public readonly List<string> Warnings = new();

        public void Log(string message) { }
        public void Warning(string message) => Warnings.Add(message);
        public void Error(string message, Exception exception = null) => Errors.Add(exception);
    }

    internal sealed class Recorder : IGameElement,
        IGameInitListener,
        IGameStartedListener,
        IGamePausedListener,
        IGameResumedListener,
        IGameStoppedListener,
        IGameTickable
    {
        public int Inits, Starts, Pauses, Resumes, Stops, Ticks;
        public Func<UniTask> OnStart;

        public UniTask OnInit(CancellationToken cancellationToken) { Inits++; return UniTask.CompletedTask; }
        public UniTask OnStartGame(CancellationToken cancellationToken) { Starts++; return OnStart?.Invoke() ?? UniTask.CompletedTask; }
        public UniTask OnPauseGame(CancellationToken cancellationToken) { Pauses++; return UniTask.CompletedTask; }
        public UniTask OnResumeGame(CancellationToken cancellationToken) { Resumes++; return UniTask.CompletedTask; }
        public UniTask OnStopGame(CancellationToken cancellationToken) { Stops++; return UniTask.CompletedTask; }
        public void Tick(float deltaTime) => Ticks++;
    }

    internal static class UniTaskTestExtensions
    {
        public static void Run(this UniTask task) => task.GetAwaiter().GetResult();
    }

    public abstract class GameLoopTestBase
    {
        internal FakeTime Time;
        internal CapturingLogger Logger;
        internal GameLoop Loop;

        [SetUp]
        public void SetUpLoop()
        {
            Time = new FakeTime();
            Logger = new CapturingLogger();
            Loop = new GameLoop(Time, Logger);
        }

        [TearDown]
        public void TearDownLoop() => Loop.Destroy();

        internal void Frame() => Loop.DriveUpdate();
    }
}
