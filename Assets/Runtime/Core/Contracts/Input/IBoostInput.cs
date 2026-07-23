using System;

namespace Planet_IO
{
    public interface IBoostInput
    {
        event Action<bool> BoostChanged;
    }
}
