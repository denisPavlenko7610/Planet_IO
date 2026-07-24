namespace PlanetIO
{
    public sealed class PointPool : ObjectPool.ObjectPool<Point>
    {
        protected override int MinimumCapacity => 60;

        protected override int MaximumPoolSize => Capacity * 2;
    }
}
