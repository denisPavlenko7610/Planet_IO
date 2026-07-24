using UnityEngine;

namespace PlanetIO
{
    public interface IContentInitializationService
    {
        bool IsReady { get; }

        Awaitable InitializeAsync();
    }
}
