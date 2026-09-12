using System;
using GoogleMobileAds.Api;
using UnityEngine;

namespace PlanetIO.Infrastructure.Ads
{
    public sealed class AdMobRewardedService : IRewardedAdsService
    {
        private const string ReleaseAdUnitId = "ca-app-pub-7173647303121367/2914802868";
        private const string TestAdUnitId = "ca-app-pub-3940256099942544/5224354917";
        private const float ReloadDelaySeconds = 30f;

        private RewardedAd _rewardedAd;
        private Action<bool> _showCallback;
        private bool _earned;

        private static string AdUnitId
        {
            get
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                return TestAdUnitId;
#else
                return ReleaseAdUnitId;
#endif
            }
        }

        public bool CanShowAd
        {
            get
            {
#if UNITY_EDITOR
                return true;
#else
                return _rewardedAd != null && _rewardedAd.IsLoaded();
#endif
            }
        }

        public void Initialize()
        {
#if UNITY_EDITOR
            return;
#else
            MobileAds.Initialize(_ => LoadAd());
#endif
        }

        public void LoadAd()
        {
#if UNITY_EDITOR
            return;
#else
            if (_rewardedAd != null && _rewardedAd.IsLoaded())
            {
                return;
            }

            _rewardedAd = new RewardedAd(AdUnitId);
            _rewardedAd.OnAdFailedToLoad += (_, _) => _ = ReloadAfterDelayAsync();
            _rewardedAd.OnAdFullScreenContentFailed += (_, _) => FinishShow(false);
            _rewardedAd.OnAdFullScreenContentClosed += (_, _) => FinishShow(_earned);
            _rewardedAd.OnUserEarnedReward += (_, _) => _earned = true;
            _rewardedAd.LoadAd(new AdRequest.Builder().Build());
#endif
        }

        public void Show(Action<bool> onCompleted)
        {
            if (onCompleted == null)
            {
                return;
            }

#if UNITY_EDITOR
            onCompleted(true);
#else
            if (!CanShowAd)
            {
                onCompleted(false);
                LoadAd();
                return;
            }

            _earned = false;
            _showCallback = onCompleted;
            _rewardedAd.Show();
#endif
        }

        private void FinishShow(bool granted)
        {
            Action<bool> callback = _showCallback;
            _showCallback = null;
            LoadAd();
            callback?.Invoke(granted);
        }

        private async Awaitable ReloadAfterDelayAsync()
        {
            await Awaitable.WaitForSecondsAsync(ReloadDelaySeconds);
            LoadAd();
        }
    }
}
