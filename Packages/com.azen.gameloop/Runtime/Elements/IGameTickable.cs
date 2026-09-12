namespace Azen.GameLoop
{
    public interface IGameTickable : IGameElement
    {
        void Tick(float deltaTime);
    }
}
