using System;
using Cysharp.Threading.Tasks;

namespace Azen.GameLoop
{
    public static class GameLoopExtensions
    {
        public static GameLoopRegistration For(this IGameLoop loop, IGameElement element)
            => new GameLoopRegistration(loop, element, GameLoopChannels.Default, 0);

        public static UniTask StartAsync(this IGameManager manager)
            => manager.StartAsync(default);

        public static UniTask PauseAsync(this IGameManager manager)
            => manager.PauseAsync(default);

        public static UniTask ResumeAsync(this IGameManager manager)
            => manager.ResumeAsync(default);

        public static UniTask StopAsync(this IGameManager manager)
            => manager.StopAsync(default);

        public static UniTask RestartAsync(this IGameManager manager)
            => manager.RestartAsync(default);

        public static ScheduleHandle Every(this IGameLoop loop, float intervalSeconds, Action action, string channel = null)
            => loop.Scheduler.Every(intervalSeconds, action, channel);

        public static ScheduleHandle Once(this IGameLoop loop, float delaySeconds, Action action, string channel = null)
            => loop.Scheduler.Once(delaySeconds, action, channel);

        public static bool Cancel(this IGameLoop loop, ScheduleHandle handle)
            => loop.Scheduler.Cancel(handle);
    }
}
