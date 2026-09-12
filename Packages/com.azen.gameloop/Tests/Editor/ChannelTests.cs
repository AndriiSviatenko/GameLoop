using NUnit.Framework;

namespace Azen.GameLoop.Tests
{
    public sealed class ChannelTests : GameLoopTestBase
    {
        private const string Gameplay = GameLoopChannels.Gameplay;

        private Recorder _recorder;

        [SetUp]
        public void SetUpRecorder()
        {
            _recorder = new Recorder();
            Loop.Add(_recorder, Gameplay);
        }

        [Test]
        public void PauseChannelTwice_NotifiesOnce()
        {
            Loop.StartAsync().Run();

            Loop.PauseChannelAsync(Gameplay, default).Run();
            Loop.PauseChannelAsync(Gameplay, default).Run();

            Assert.AreEqual(1, _recorder.Pauses);
        }

        [Test]
        public void PausedChannel_StopsTicking()
        {
            Loop.StartAsync().Run();
            Frame();

            Loop.PauseChannelAsync(Gameplay, default).Run();
            Frame();

            Assert.AreEqual(1, _recorder.Ticks);
        }

        [Test]
        public void GlobalPause_DoesNotNotifyAlreadyPausedChannel()
        {
            Loop.StartAsync().Run();
            Loop.PauseChannelAsync(Gameplay, default).Run();

            Loop.PauseAsync().Run();

            Assert.AreEqual(1, _recorder.Pauses);
        }

        [Test]
        public void GlobalResume_KeepsPausedChannelPaused()
        {
            Loop.StartAsync().Run();
            Loop.PauseChannelAsync(Gameplay, default).Run();
            Loop.PauseAsync().Run();

            Loop.ResumeAsync().Run();
            Frame();

            Assert.AreEqual(0, _recorder.Resumes);
            Assert.AreEqual(0, _recorder.Ticks);
            Assert.IsTrue(Loop.GetChannelState(Gameplay).IsPaused);
        }

        [Test]
        public void ChannelResume_AfterGlobalResume_NotifiesAndTicks()
        {
            Loop.StartAsync().Run();
            Loop.PauseChannelAsync(Gameplay, default).Run();
            Loop.PauseAsync().Run();
            Loop.ResumeAsync().Run();

            Loop.ResumeChannelAsync(Gameplay, default).Run();
            Frame();

            Assert.AreEqual(1, _recorder.Resumes);
            Assert.AreEqual(1, _recorder.Ticks);
        }

        [Test]
        public void PauseAndResume_AreBalanced_WhenChannelPausedDuringGlobalPause()
        {
            Loop.StartAsync().Run();
            Loop.PauseAsync().Run();
            Loop.PauseChannelAsync(Gameplay, default).Run();
            Loop.ResumeAsync().Run();
            Loop.ResumeChannelAsync(Gameplay, default).Run();

            Assert.AreEqual(1, _recorder.Pauses);
            Assert.AreEqual(1, _recorder.Resumes);
            Assert.IsTrue(Loop.GetChannelState(Gameplay).IsPlaying);
        }

        [Test]
        public void ChannelState_FollowsGlobalState_WhenGameIsNotPlaying()
        {
            Loop.PauseChannelAsync(Gameplay, default).Run();

            Assert.IsTrue(Loop.GetChannelState(Gameplay).IsOff);
            Assert.AreEqual(0, _recorder.Pauses);
        }

        [Test]
        public void ChannelStateChanged_IsRaisedOncePerChange()
        {
            var raised = 0;
            Loop.ChannelStateChanged += (_, _) => raised++;
            Loop.StartAsync().Run();

            Loop.PauseChannelAsync(Gameplay, default).Run();
            Loop.PauseChannelAsync(Gameplay, default).Run();
            Loop.ResumeChannelAsync(Gameplay, default).Run();
            Loop.ResumeChannelAsync(Gameplay, default).Run();

            Assert.AreEqual(2, raised);
        }

        [Test]
        public void Restart_ClearsChannelPause()
        {
            Loop.StartAsync().Run();
            Loop.PauseChannelAsync(Gameplay, default).Run();

            Loop.RestartAsync().Run();
            Frame();

            Assert.IsTrue(Loop.GetChannelState(Gameplay).IsPlaying);
            Assert.AreEqual(1, _recorder.Ticks);
        }
    }
}
