using System;
using System.Collections;
using UnityEngine;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace PlanetIO.Tests
{
    public sealed partial class GameFlowTests
    {
        [UnityTest]
        [Timeout(45000)]
        public IEnumerator RewardedAds_ConsentCallbacksHaveAnActiveMainThreadExecutor()
        {
            yield return OpenMenu();

            GameObject executor = GameObject.Find("MobileAdsMainThreadExecuter");
            Assert.That(executor, Is.Not.Null,
                "Consent callbacks are queued before MobileAds.Initialize, so the executor must already exist.");
            Component component = executor.GetComponent("MobileAdsEventExecutor");
            Assert.That(component, Is.Not.Null);
            bool executed = false;
            component.GetType().GetMethod("ExecuteInUpdate").Invoke(null,
                new object[] { (Action)(() => executed = true) });
            yield return null;
            Assert.That(executed, Is.True);
        }
    }
}
