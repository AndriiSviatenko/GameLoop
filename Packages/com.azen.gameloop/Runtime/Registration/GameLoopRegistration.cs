namespace Azen.GameLoop
{
    public readonly struct GameLoopRegistration
    {
        private readonly IGameLoop _loop;
        private readonly IGameElement _element;
        private readonly string _channel;
        private readonly int _priority;

        internal GameLoopRegistration(IGameLoop loop, IGameElement element, string channel, int priority)
        {
            _loop = loop;
            _element = element;
            _channel = channel;
            _priority = priority;
        }

        public GameLoopRegistration On(string channel)
            => new GameLoopRegistration(_loop, _element, channel ?? GameLoopChannels.Default, _priority);

        public GameLoopRegistration Gameplay()  => On(GameLoopChannels.Gameplay);
        public GameLoopRegistration UI()        => On(GameLoopChannels.UI);
        public GameLoopRegistration Cutscene()  => On(GameLoopChannels.Cutscene);
        public GameLoopRegistration Default()   => On(GameLoopChannels.Default);

        public GameLoopRegistration Priority(int priority)
            => new GameLoopRegistration(_loop, _element, _channel, priority);

        public GameLoopRegistration Low()      => Priority(GameLoopPriority.Low);
        public GameLoopRegistration Normal()   => Priority(GameLoopPriority.Normal);
        public GameLoopRegistration High()     => Priority(GameLoopPriority.High);
        public GameLoopRegistration Critical() => Priority(GameLoopPriority.Critical);

        public GameLoopElementHandle Add() => _loop.Add(_element, _channel, _priority);
    }
}
