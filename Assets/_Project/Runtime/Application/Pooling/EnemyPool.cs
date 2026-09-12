using PlanetIO.Pooling;
namespace PlanetIO
{
    public sealed class EnemyPool : ObjectPool<Enemy>
    {
        public const int MinimumOpponentCount = 5;

        protected override int MinimumCapacity => MinimumOpponentCount;
    }
}
