using PlanetIO.Core.Contracts.Loading;
using UnityEngine;

namespace PlanetIO.UI.Loading
{
    public sealed class GameLoadingView : MonoBehaviour, IGameLoadingView
    {
        [SerializeField] private GameObject _overlayRoot;

        public void Show()
        {
            if (_overlayRoot != null)
            {
                _overlayRoot.SetActive(true);
            }
        }

        public void Hide()
        {
            if (_overlayRoot != null)
            {
                _overlayRoot.SetActive(false);
            }
        }
    }
}
