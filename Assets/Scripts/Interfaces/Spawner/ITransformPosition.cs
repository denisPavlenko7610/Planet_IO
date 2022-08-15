namespace Spawner
{
    public interface ITransformPosition<in T>
    {
        void SetTransform(T @object, float randomScale);
    }
}