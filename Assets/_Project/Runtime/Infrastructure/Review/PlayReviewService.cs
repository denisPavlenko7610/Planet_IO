using System;
using PlanetIO.Infrastructure.Services;
using UnityEngine;
#if UNITY_ANDROID
using Google.Play.Review;
#endif

namespace PlanetIO.Infrastructure.Review
{
    public sealed class PlayReviewService : IReviewService
    {
#if UNITY_ANDROID
        private ReviewManager _manager;
#endif
        private bool _busy;

        public bool IsAvailable => PlatformServiceIds.IsPlayableDevice;

        public void Request()
        {
#if UNITY_ANDROID
            if (!IsAvailable || _busy)
            {
                return;
            }

            _busy = true;
            try
            {
                _manager ??= new ReviewManager();
                var request = _manager.RequestReviewFlow();
                request.Completed += operation =>
                {
                    if (!operation.IsSuccessful)
                    {
                        _busy = false;
                        Debug.LogWarning($"Review flow request failed: {operation.Error}");
                        return;
                    }

                    var launch = _manager.LaunchReviewFlow(operation.GetResult());
                    launch.Completed += _ => _busy = false;
                };
            }
            catch (Exception exception)
            {
                _busy = false;
                Debug.LogWarning($"Review flow failed: {exception.Message}");
            }
#endif
        }
    }
}
