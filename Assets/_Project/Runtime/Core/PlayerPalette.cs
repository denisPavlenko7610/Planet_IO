using System.Collections.Generic;
using UnityEngine;

namespace PlanetIO
{
    public static class PlayerPalette
    {
        private static readonly List<Color32> ColorsList = new()
        {
            new Color32(0x4f, 0xc3, 0xf7, 0xff),
            new Color32(0x81, 0xc7, 0x84, 0xff),
            new Color32(0xba, 0x68, 0xc8, 0xff),
            new Color32(0xff, 0xb7, 0x4d, 0xff),
            new Color32(0xf0, 0x62, 0x92, 0xff),
            new Color32(0xff, 0xf1, 0x76, 0xff),
        };

        public static IReadOnlyList<Color32> Colors => ColorsList;

        public static Color32 GetColor(int index) => ColorsList[Mathf.Abs(index) % ColorsList.Count];

        public static int IndexOf(Color32 color)
        {
            for (int index = 0; index < ColorsList.Count; index++)
            {
                if (ColorsList[index].Equals(color))
                {
                    return index;
                }
            }

            return -1;
        }
    }
}
