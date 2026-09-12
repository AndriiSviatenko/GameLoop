namespace Azen.GameLoop.Infrastructure
{
    public interface ITimeProvider
    {
        float DeltaTime { get; }
        float FixedDeltaTime { get; }
    }
}
