using System;
using UnityEngine;

namespace PlanetIO
{
    public interface IWorldEvents
    {
        event Action<CosmicEventType> Warned;
        event Action<CosmicEventType> Started;
        event Action<CosmicEventType> Ended;

        DailyModifier Modifier { get; }
        bool HasMarker { get; }
        CosmicEventType MarkerType { get; }
        Vector2 MarkerPosition { get; }
    }
}
