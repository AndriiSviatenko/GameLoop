using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;

namespace Azen.GameLoop.Tests
{
    public sealed class TransitionTests : GameLoopTestBase
    {
        [Test]
        public void Start_InitializesStartsAndPlays()
        {
            var recorder = new Recorder();
            Loop.Add(recorder);

            Loop.StartAsync().Run();

            Assert.AreEqual(1, recorder.Inits);
            Assert.AreEqual(1, recorder.Starts);
            Assert.IsTrue(Loop.State.IsPlaying);
        }

        [Test]
        public void TransitionRequestedDuringPendingTransition_IsQueued()
        {
            var gate = new UniTaskCompletionSource();
            var recorder = new Recorder { OnStart = () => gate.Task };
            Loop.Add(recorder);

            var start = Loop.StartAsync();
            var pause = Loop.PauseAsync();
            Assert.IsTrue(Loop.State.IsOff);

            gate.TrySetResult();
            start.Run();
            pause.Run();

            Assert.IsTrue(Loop.State.IsPaused);
            Assert.AreEqual(1, recorder.Pauses);
        }

        [Test]
        public void SynchronousCallFromListener_Throws_WhenThrowOnReentrancy()
        {
            UniTask inner = default;
            var recorder = new Recorder();
            recorder.OnStart = () =>
            {
                inner = Loop.PauseAsync();
                return UniTask.CompletedTask;
            };
            Loop.Add(recorder);

            Loop.StartAsync().Run();

            Assert.Throws<InvalidOperationException>(() => inner.Run());
            Assert.IsTrue(Loop.State.IsPlaying);
        }

        [Test]
        public void SynchronousCallFromListener_IsIgnored_WhenThrowOnReentrancyDisabled()
        {
            Loop.ThrowOnReentrancy = false;
            UniTask inner = default;
            var recorder = new Recorder();
            recorder.OnStart = () =>
            {
                inner = Loop.PauseAsync();
                return UniTask.CompletedTask;
            };
            Loop.Add(recorder);

            Loop.StartAsync().Run();

            Assert.DoesNotThrow(() => inner.Run());
            Assert.IsTrue(Loop.State.IsPlaying);
            Assert.AreEqual(1, Logger.Warnings.Count);
        }

        [Test]
        public void CancelledQueuedTransition_ReleasesQueue()
        {
            var gate = new UniTaskCompletionSource();
            Loop.Add(new Recorder { OnStart = () => gate.Task });
            var start = Loop.StartAsync();

            var cts = new CancellationTokenSource();
            var pause = Loop.PauseAsync(cts.Token);
            cts.Cancel();
            Assert.Catch<OperationCanceledException>(() => pause.Run());

            gate.TrySetResult();
            start.Run();
            Loop.StopAsync().Run();

            Assert.IsTrue(Loop.State.IsStopped);
        }

        [Test]
        public void PauseReasons_ResumeOnlyWhenAllReasonsRemoved()
        {
            Loop.StartAsync().Run();

            Loop.PauseFor(PauseReason.Menu, default).Run();
            Loop.PauseFor(PauseReason.Dialog, default).Run();
            Loop.ResumeFor(PauseReason.Dialog, default).Run();
            Assert.IsTrue(Loop.State.IsPaused);

            Loop.ResumeFor(PauseReason.Menu, default).Run();
            Assert.IsTrue(Loop.State.IsPlaying);
        }

        [Test]
        public void Transitions_AreIgnored_AfterDestroy()
        {
            Loop.Destroy();

            Loop.StartAsync().Run();

            Assert.IsTrue(Loop.State.IsOff);
        }
    }
}
