using PlanetIO.Pooling;
namespace PlanetIO
{
    public sealed class FoodPool : ObjectPool<Food>
    {
        protected override int MinimumCapacity => 90;

        protected override int MaximumPoolSize => Capacity * 2;

        protected override void ActivatePooledObject(Food pooledObject)
        {
            pooledObject.ResetClaim();
            base.ActivatePooledObject(pooledObject);
        }
    }
}
