using System;
using System.Linq;
using NUnit.Framework;

namespace Azen.GameLoop.Tests
{
    public sealed class ErrorHandlingTests : GameLoopTestBase
    {
        private sealed class ThrowingTicker : IGameTickable
        {
            public readonly Exception Error = new InvalidOperationException("boom");

            public void Tick(float deltaTime) => throw Error;
        }

        [Test]
        public void TickError_IsPassedToLogger_WithOriginalException()
        {
            var ticker = new ThrowingTicker();
            Loop.Add(ticker);
            Loop.StartAsync().Run();

            Frame();

            Assert.AreSame(ticker.Error, Logger.Errors.Single());
        }

        [Test]
        public void ThrowOnErrors_RethrowsOriginalException_WithOriginalStackTrace()
        {
            Loop.Destroy();
            Loop = new GameLoop(Time, Logger, throwOnErrors: true);
            var ticker = new ThrowingTicker();
            Loop.Add(ticker);
            Loop.StartAsync().Run();

            var thrown = Assert.Throws<InvalidOperationException>(Frame);

            Assert.AreSame(ticker.Error, thrown);
            StringAssert.Contains(nameof(ThrowingTicker), thrown.StackTrace);
        }

        [Test]
        public void TickError_DoesNotStopOtherSystems()
        {
            var recorder = new Recorder();
            Loop.Add(new ThrowingTicker(), priority: GameLoopPriority.High);
            Loop.Add(recorder, priority: GameLoopPriority.Low);
            Loop.StartAsync().Run();

            Frame();

            Assert.AreEqual(1, recorder.Ticks);
        }
    }
}
