namespace Azen.GameLoop
{
    public interface IGameFixedTickable : IGameElement
    {
        void FixedTick(float fixedDeltaTime);
    }
}
