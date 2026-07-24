namespace Planet_IO
{
    public sealed class EnemyPool : ObjectPool.ObjectPool<Enemy>
    {
        public const int MinimumOpponentCount = 10;

        protected override int MinimumCapacity => MinimumOpponentCount;
    }
}
