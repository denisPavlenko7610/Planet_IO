namespace PlanetIO
{
    public sealed class EnemyPool : ObjectPool.ObjectPool<Enemy>
    {
        public const int MinimumOpponentCount = 5;

        protected override int MinimumCapacity => MinimumOpponentCount;
    }
}
