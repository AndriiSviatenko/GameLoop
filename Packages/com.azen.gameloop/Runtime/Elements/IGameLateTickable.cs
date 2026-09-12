namespace Azen.GameLoop
{
    public interface IGameLateTickable : IGameElement
    {
        void LateTick(float deltaTime);
    }
}
