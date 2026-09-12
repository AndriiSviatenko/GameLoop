using UnityEngine;

namespace Azen.GameLoop.Demo
{
    public sealed class FallingBall : GameLoopElement, IGameTickable
    {
        private IGameLoop _loop;
        private BallRegistry _registry;
        private float _lifetime;

        public Vector3 Velocity { get; set; }

        private void Awake() => Configure(GameLoopChannels.Gameplay);

        public void Construct(IGameLoop loop, BallRegistry registry)
        {
            _loop = loop;
            _registry = registry;
            _registry.Add(this);
            TryRegister();
        }

        protected override IGameLoop ResolveLoop() => _loop;

        protected override void OnDestroy()
        {
            _registry?.Remove(this);
            base.OnDestroy();
        }

        public void Tick(float deltaTime)
        {
            _lifetime += deltaTime;

            var velocity = Velocity;
            velocity.y += DemoConfig.Ball.Gravity * deltaTime;
            var position = transform.position + velocity * deltaTime;

            if (position.y <= DemoConfig.Ball.FloorY)
            {
                position.y = DemoConfig.Ball.FloorY;
                velocity.y = -velocity.y * DemoConfig.Ball.BounceRestitution;
                if (Mathf.Abs(velocity.y) < DemoConfig.Ball.MinBounceVelocity)
                    velocity.y = 0f;
            }

            Velocity = velocity;
            transform.position = position;

            if (_lifetime > DemoConfig.Ball.LifetimeSeconds)
                Destroy(gameObject);
        }
    }
}
