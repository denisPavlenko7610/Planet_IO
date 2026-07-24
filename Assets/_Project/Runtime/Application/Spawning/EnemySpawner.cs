namespace PlanetIO
{
    public sealed class EnemySpawner : Spawner<Enemy>, IRespawnService<Enemy>
    {
        public void Respawn(Enemy enemy) => RespawnObject(enemy);
    }
}
