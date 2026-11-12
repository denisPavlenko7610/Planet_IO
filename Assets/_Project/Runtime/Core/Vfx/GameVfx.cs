using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace PlanetIO
{
    public interface IGameVfxPlayer
    {
        void PlayEat(Vector2 position, Color color, float size);
        void PlayDeath(Vector2 position, Color color, float size);
        void PlayImpact(Vector2 position, float size);
    }

    [AutoStaticsCleanup]
    public static partial class GameVfx
    {
        public static IGameVfxPlayer Player { get; set; }

        public static void Eat(Vector2 position, Color color, float size) => Player?.PlayEat(position, color, size);

        public static void Death(Vector2 position, Color color, float size) => Player?.PlayDeath(position, color, size);

        public static void Impact(Vector2 position, float size) => Player?.PlayImpact(position, size);
    }
}
