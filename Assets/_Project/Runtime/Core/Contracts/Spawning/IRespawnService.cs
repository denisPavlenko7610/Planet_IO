namespace PlanetIO
{
    public interface IRespawnService<in T>
    {
        void Respawn(T entity);
    }
}
