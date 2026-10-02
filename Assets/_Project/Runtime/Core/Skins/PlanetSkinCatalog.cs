using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlanetIO
{
    [CreateAssetMenu(menuName = "Planet IO/Skins/Planet Skin Catalog")]
    public sealed class PlanetSkinCatalog : ScriptableObject
    {
        [Serializable]
        public struct Skin
        {
            public string Id;
            public Sprite Sprite;
            public bool Free;
        }

        [SerializeField] private List<Skin> _skins = new();

        public int Count => _skins.Count;

        public Skin Get(int index)
        {
            if (!IsValid(index))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(index), index, $"Skin index {index} is out of range [0, {_skins.Count - 1}].");
            }

            return _skins[index];
        }

        public bool TryGet(int index, out Skin skin)
        {
            if (!IsValid(index))
            {
                skin = default;
                return false;
            }

            skin = _skins[index];
            return true;
        }

        public bool IsValid(int index) => index >= 0 && index < _skins.Count;

        public int IndexOf(string id)
        {
            for (int index = 0; index < _skins.Count; index++)
            {
                if (_skins[index].Id == id)
                {
                    return index;
                }
            }

            return -1;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_skins.Count == 0)
            {
                Debug.LogWarning($"[PlanetSkinCatalog] '{name}' has no skins configured.", this);
                return;
            }

            foreach (Skin skin in _skins)
            {
                if (string.IsNullOrWhiteSpace(skin.Id) || skin.Sprite == null)
                {
                    Debug.LogWarning($"[PlanetSkinCatalog] '{name}' contains a skin with an empty Id or missing Sprite.", this);
                    break;
                }
            }
        }
#endif
    }
}
