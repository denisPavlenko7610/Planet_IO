using System;

namespace PlanetIO
{
    public interface IRewardedAdsService
    {
        bool CanShowAd { get; }

        void Initialize();

        void LoadAd();

        void Show(Action<bool> onCompleted);
    }
}
