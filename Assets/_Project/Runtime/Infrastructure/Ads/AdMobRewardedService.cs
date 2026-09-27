using System;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using GoogleMobileAds.Ump.Api;
using UnityEngine;

namespace PlanetIO.Infrastructure.Ads
{
    public sealed class AdMobRewardedService : IRewardedAdsService, IAdPrivacyService
    {
        private const string ReleaseAdUnitId = "ca-app-pub-7173647303121367/2914802868";
        private const string TestAdUnitId = "ca-app-pub-3940256099942544/5224354917";
        private const float ReloadDelaySeconds = 30f;

        private RewardedAd _rewardedAd;
        private Action<bool> _showCallback;
        private bool _earned;
        private bool _initialized;
        private bool _loading;

        private static bool UseEditorStub => UnityEngine.Application.isEditor;

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

        public event Action PrivacyOptionsChanged;

        public bool IsPrivacyOptionsRequired =>
            !UseEditorStub &&
            ConsentInformation.PrivacyOptionsRequirementStatus == PrivacyOptionsRequirementStatus.Required;

        public bool CanShowAd => UseEditorStub || _rewardedAd != null && _rewardedAd.CanShowAd();

        public void Initialize()
        {
            if (UseEditorStub || _initialized)
            {
                return;
            }

            _initialized = true;
            ConsentInformation.Update(
                new ConsentRequestParameters(),
                error => MobileAdsEventExecutor.ExecuteInUpdate(() => OnConsentInfoUpdated(error)));
        }

        public void LoadAd()
        {
            if (UseEditorStub ||
                _loading ||
                !ConsentInformation.CanRequestAds() ||
                _rewardedAd != null && _rewardedAd.CanShowAd())
            {
                return;
            }

            DestroyAd();
            _loading = true;
            RewardedAd.Load(
                AdUnitId,
                new AdRequest(),
                (ad, error) => MobileAdsEventExecutor.ExecuteInUpdate(() => OnAdLoaded(ad, error)));
        }

        public void Show(Action<bool> onCompleted)
        {
            if (onCompleted == null)
            {
                return;
            }

            if (UseEditorStub)
            {
                onCompleted(true);
                return;
            }

            if (!CanShowAd || _showCallback != null)
            {
                onCompleted(false);
                LoadAd();
                return;
            }

            _earned = false;
            _showCallback = onCompleted;
            _rewardedAd.Show(_ => MobileAdsEventExecutor.ExecuteInUpdate(() => _earned = true));
        }

        public void ShowPrivacyOptions()
        {
            if (!IsPrivacyOptionsRequired)
            {
                return;
            }

            ConsentForm.ShowPrivacyOptionsForm(formError => MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                if (formError != null)
                {
                    GameLogger.LogWarning($"Privacy options form failed: {formError.Message}");
                }

                PrivacyOptionsChanged?.Invoke();
                LoadAd();
            }));
        }

        private void OnConsentInfoUpdated(FormError updateError)
        {
            if (updateError != null)
            {
                GameLogger.LogWarning($"Consent info update failed: {updateError.Message}");
            }

            ConsentForm.LoadAndShowConsentFormIfRequired(formError => MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                if (formError != null)
                {
                    GameLogger.LogWarning($"Consent form failed: {formError.Message}");
                }

                PrivacyOptionsChanged?.Invoke();

                if (ConsentInformation.CanRequestAds())
                {
                    MobileAds.Initialize(_ => MobileAdsEventExecutor.ExecuteInUpdate(LoadAd));
                }
            }));
        }

        private void OnAdLoaded(RewardedAd ad, LoadAdError error)
        {
            _loading = false;

            if (error != null || ad == null)
            {
                GameLogger.LogWarning($"Rewarded ad failed to load: {error?.GetMessage()}");
                _ = ReloadAfterDelayAsync();
                return;
            }

            _rewardedAd = ad;
            _rewardedAd.OnAdFullScreenContentClosed +=
                () => MobileAdsEventExecutor.ExecuteInUpdate(() => FinishShow(_earned));
            _rewardedAd.OnAdFullScreenContentFailed +=
                _ => MobileAdsEventExecutor.ExecuteInUpdate(() => FinishShow(false));
        }

        private void FinishShow(bool granted)
        {
            Action<bool> callback = _showCallback;
            _showCallback = null;
            DestroyAd();
            LoadAd();
            callback?.Invoke(granted);
        }

        private void DestroyAd()
        {
            _rewardedAd?.Destroy();
            _rewardedAd = null;
        }

        private async Awaitable ReloadAfterDelayAsync()
        {
            await Awaitable.WaitForSecondsAsync(ReloadDelaySeconds);
            LoadAd();
        }
    }
}
