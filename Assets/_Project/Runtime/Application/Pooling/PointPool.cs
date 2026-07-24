namespace PlanetIO
{
    public sealed class PointPool : ObjectPool.ObjectPool<Point>
    {
        protected override int MaximumPoolSize => Capacity * 2;
    }
}
