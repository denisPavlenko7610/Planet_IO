namespace Planet_IO
{
    public interface IRespawnService<in T>
    {
        void Respawn(T entity);
    }
}
