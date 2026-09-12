using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Azen.GameLoop.Demo
{
    public sealed class ScoreSystem : IGameElement, IGameInitListener, IGameTickable
    {
        private float _score;

        public int CurrentScore { get; private set; }

        public UniTask OnInit(CancellationToken cancellationToken)
        {
            _score = 0f;
            CurrentScore = 0;
            return UniTask.CompletedTask;
        }

        public void Tick(float deltaTime)
        {
            _score += deltaTime * DemoConfig.Score.PointsPerSecond;
            CurrentScore = Mathf.FloorToInt(_score);
        }
    }
}
