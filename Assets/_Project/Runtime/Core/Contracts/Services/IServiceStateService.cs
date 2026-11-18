using System;
using System.Threading;
using UnityEngine;

namespace PlanetIO
{
    public interface IServiceStateService
    {
        event Action Changed;

        bool IsLoaded { get; }
        ServiceState State { get; }

        Awaitable InitializeAsync(CancellationToken cancellationToken = default);

        void MarkDirty();

        Awaitable FlushAsync();
    }
}
