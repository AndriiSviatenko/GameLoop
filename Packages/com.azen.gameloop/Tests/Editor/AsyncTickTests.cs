using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;

namespace Azen.GameLoop.Tests
{
    public sealed class AsyncTickTests : GameLoopTestBase
    {
        private sealed class AsyncTicker : IGameAsyncTickable
        {
            public int Calls;
            public UniTaskCompletionSource Pending;
            public CancellationToken LastToken;

            public UniTask TickAsync(float deltaTime, CancellationToken cancellationToken)
            {
                Calls++;
                LastToken = cancellationToken;
                return Pending?.Task ?? UniTask.CompletedTask;
            }
        }

        [Test]
        public void AsyncTickable_IsTickedEveryFrame_WhilePlaying()
        {
            var ticker = new AsyncTicker();
            Loop.Add(ticker);
            Loop.StartAsync().Run();

            Frame();
            Frame();

            Assert.AreEqual(2, ticker.Calls);
        }

        [Test]
        public void AsyncTickable_IsNotTicked_BeforeStart()
        {
            var ticker = new AsyncTicker();
            Loop.Add(ticker);

            Frame();

            Assert.AreEqual(0, ticker.Calls);
        }

        [Test]
        public void AsyncTickable_DoesNotOverlap_WhilePreviousTickIsPending()
        {
            var ticker = new AsyncTicker { Pending = new UniTaskCompletionSource() };
            Loop.Add(ticker);
            Loop.StartAsync().Run();

            Frame();
            Frame();
            Assert.AreEqual(1, ticker.Calls);

            var pending = ticker.Pending;
            ticker.Pending = null;
            pending.TrySetResult();
            Frame();

            Assert.AreEqual(2, ticker.Calls);
        }

        [Test]
        public void AsyncTickable_TokenIsCancelled_OnDestroy()
        {
            var ticker = new AsyncTicker { Pending = new UniTaskCompletionSource() };
            Loop.Add(ticker);
            Loop.StartAsync().Run();
            Frame();

            Loop.Destroy();

            Assert.IsTrue(ticker.LastToken.IsCancellationRequested);
        }
    }
}
