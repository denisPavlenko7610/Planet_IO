using UnityEngine;

namespace Planet_IO
{
    public interface IContentInitializationService
    {
        bool IsReady { get; }

        Awaitable InitializeAsync();
    }
}
