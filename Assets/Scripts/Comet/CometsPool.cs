using Pool;

namespace PlanetIO
{
    public class CometsPool : ObjectPool<Comet>
    {
        private void OnDestroy()
        {
            
        }
    }
}