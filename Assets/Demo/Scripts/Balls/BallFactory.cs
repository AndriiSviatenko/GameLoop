using UnityEngine;

namespace Azen.GameLoop.Demo
{
    public sealed class BallFactory
    {
        private readonly IGameLoop _loop;
        private readonly BallRegistry _registry;

        public BallFactory(IGameLoop loop, BallRegistry registry)
        {
            _loop = loop;
            _registry = registry;
        }

        public FallingBall Create(Vector3 position)
        {
            var ballObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ballObject.name = "Ball";
            ballObject.transform.position = position;
            ballObject.GetComponent<Renderer>().material.color = new Color(
                Random.value,
                Random.Range(DemoConfig.Colors.MinGreenChannel, DemoConfig.Colors.MaxGreenChannel),
                Random.value);

            var ball = ballObject.AddComponent<FallingBall>();
            ball.Construct(_loop, _registry);
            return ball;
        }
    }
}
