using NUnit.Framework;

namespace Azen.GameLoop.Tests
{
    public sealed class SchedulerTests : GameLoopTestBase
    {
        [Test]
        public void TimerOnChannelThatTicksWhenPaused_FiresWhileGloballyPaused()
        {
            Loop.SetChannelTickWhenPaused(GameLoopChannels.UI, true);
            Loop.StartAsync().Run();
            Loop.PauseAsync().Run();
            var fired = 0;
            Loop.Once(0.05f, () => fired++, GameLoopChannels.UI);

            Frame();

            Assert.AreEqual(1, fired);
        }

        [Test]
        public void GameplayTimer_WaitsWhileGloballyPaused()
        {
            Loop.StartAsync().Run();
            Loop.PauseAsync().Run();
            var fired = 0;
            Loop.Once(0.05f, () => fired++, GameLoopChannels.Gameplay);

            Frame();
            Assert.AreEqual(0, fired);

            Loop.ResumeAsync().Run();
            Frame();
            Assert.AreEqual(1, fired);
        }

        [Test]
        public void TimerWithoutChannel_WaitsWhileGloballyPaused()
        {
            Loop.StartAsync().Run();
            Loop.PauseAsync().Run();
            var fired = 0;
            Loop.Once(0f, () => fired++);

            Frame();

            Assert.AreEqual(0, fired);
        }

        [Test]
        public void GameplayTimer_WaitsWhileChannelPaused()
        {
            Loop.StartAsync().Run();
            Loop.PauseChannelAsync(GameLoopChannels.Gameplay, default).Run();
            var fired = 0;
            Loop.Once(0f, () => fired++, GameLoopChannels.Gameplay);

            Frame();

            Assert.AreEqual(0, fired);
        }

        [Test]
        public void ChannelTimer_UsesChannelTimeScale()
        {
            Loop.SetChannelTimeScale(GameLoopChannels.Gameplay, 0.5f);
            Loop.StartAsync().Run();
            var fired = 0;
            Loop.Every(0.09f, () => fired++, GameLoopChannels.Gameplay);

            Frame();
            Assert.AreEqual(0, fired);

            Frame();
            Assert.AreEqual(1, fired);
        }

        [Test]
        public void OnceCallback_ThatClearsScheduler_DoesNotThrow()
        {
            Loop.StartAsync().Run();
            Loop.Every(1f, () => { });
            Loop.Once(0f, () => Loop.Scheduler.Clear());

            Assert.DoesNotThrow(Frame);
            Assert.AreEqual(0, Loop.Scheduler.Count);
        }

        [Test]
        public void Once_FiresOnlyOnce()
        {
            Loop.StartAsync().Run();
            var fired = 0;
            Loop.Once(0f, () => fired++);

            Frame();
            Frame();

            Assert.AreEqual(1, fired);
        }
    }
}
