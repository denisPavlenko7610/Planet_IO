using System;

namespace PlanetIO
{
    public interface IBoostInput
    {
        event Action<bool> BoostChanged;
    }
}
